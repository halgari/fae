using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Wyld.Expressions;
using Expression = Wyld.Expressions.Expression;

namespace Wyld
{
    public class Compiler2
    {
        private ImmutableStack<ImmutableDictionary<string, ILocal>> _locals;
        public ConcurrentDictionary<Symbol, object> Globals = new();

        public ImmutableStack<string> Namespaces = ImmutableStack<string>.Empty.Push("scratch");
        
        public Compiler2()
        {
            _locals = ImmutableStack<ImmutableDictionary<string, ILocal>>.Empty.Push(ImmutableDictionary<string, ILocal>.Empty);
        }

        /// <summary>
        /// Compiles and executes the given form. 
        /// </summary>
        /// <returns></returns>
        public IInvokableArity<object> Compile(object form)
        {
            var expr = CompileForm(form);
            var emitter = new Emitter();
            return emitter.EmitTopLevel(expr);
        }

        private IExpression CompileForm(object? form)
        {
            return form switch
            {
                Cons c => CompileSexpr(c),
                Symbol s => CompileSymbol(s),
                null => Expression.NullConstant(typeof(object)),
                _ => Expression.Constant(form, form.GetType())
            };
        }
        
        private IExpression CompileSymbol(Symbol symbol)
        {
            if (symbol.Namespace == null)
            {
                if (!_locals.IsEmpty && _locals.First().TryGetValue(symbol.Name, out var expr))
                    return expr;

                var rsym = ResolveSymbol(symbol);
                if (Environment.TryResolveBox(rsym, out var box))
                {
                    return Expression.Field(Expression.Constant(box, box.GetType()), box.GetType().GetField("Value")!);
                }
            }

            throw new Exception($"Undefined symbol: {symbol}");
        }
        
        private IExpression CompileSexpr(Cons cons)
        {
            var head = cons.Head;
            switch (head)
            {
                case Symbol {Namespace: "sys"} symbol:
                    return CompileBuiltin(symbol!, cons.Tail);
                case Symbol {Namespace: null, Name: "fn"}:
                    return CompileLambda(cons.Tail!);
                case Symbol {Namespace: null, Name: "def"}:
                    return CompileDef(cons.Tail!.ToArray());
                case Symbol {Namespace: null, Name: "defn"}:
                    return CompileDefn(cons.Tail!.ToArray());
                case Symbol {Namespace: null, Name: "if"}:
                    return CompileIf(cons.Tail!.ToArray());
                case Symbol {Namespace: null} s when s.Name.StartsWith(".-"):
                    return CompilePropertyLookup(cons);
            }
            
            var fn = CompileForm(head);
            var args = cons.Tail!.Select(CompileForm).ToArray();
            return Expression.Invoke(fn, args);
        }
        
        private IExpression CompileDef(object[] args)
        {
            var sym = (Symbol)args[0];
            var boxType = TypeFromSymbol(sym.Meta[KW.Type]);
            var val = args[1];
            var box = Environment.DefineBox(ResolveSymbol(sym), boxType);
            var valExpr = CompileForm(val);
            return Expression.Assign(Expression.Field(Expression.Constant(box, box.GetType()), box.GetType().GetField("Value")!), valExpr);
        }
        
        private IExpression CompileDefn(object[] argv)
        {
            var val = CompileLambda(Cons.FromArray(argv)!);
            var sym = (Symbol) argv[0];
            var box = Environment.DefineBox(ResolveSymbol(sym), val.Type);
            return Expression.Assign(Expression.Field(Expression.Constant(box, box.GetType()), box.GetType().GetField("Value")!), val);
        }
        
        private Lambda CompileLambda(Cons form)
        {
            bool haveName = false;
            if (form.Head is Symbol fnName)
            {
                form = form.Tail!;
                haveName = true;
            }
            else
            {
                fnName = Symbol.Parse("_unknown_");
            }

            var arities = new List<(Vector, Cons)>();

            if (form.Head is Vector argv)
            {
                arities.Add((argv, form.Tail!));
            }
            else
            {
                Cons? remain = form;
                arities.Add(((Vector)remain.Head, (Cons)remain.Tail!.Head!));
            }

            var atypes = arities.Select(arity =>
            {
                var (vector, item2) = arity;
                var ret = TypeFromSymbol(vector.Meta[KW.Type]);
                var args = vector.Select(a => TypeFromSymbol(((Symbol) a).Meta[KW.Type]));
                return new Type[] {ret}.Concat(args).ToArray();
            }).ToArray();

            var fnType = Lambda.GetCombinationType(atypes);

            var carities = new List<Arity>();

            using var _l_ = WithLocals(_locals.Peek().Select(l => (ILocal)Expression.FreeVariable(l.Value)).ToArray());
            foreach (var (args, body) in arities)
            {
                var retType = TypeFromSymbol(args.Meta[KW.Type]);
                var param = args.Select((a, idx) =>
                {
                    var sym = (Symbol) a;
                    return Expression.Parameter(sym.Name, TypeFromSymbol(sym.Meta[KW.Type]), idx);
                }).ToArray();
                using var _this = WithLocals(Expression.This(fnName.Name, fnType));
                using var _ = WithLocals(param);
                var expr = CompileDo(body.ToArray());
                carities.Add(new Arity(param, expr));
            }

            var lambda = Expression.Lambda(fnName.Name, carities.ToArray());
            return lambda;
        }
        
        private IDisposable WithLocals(params ILocal[] exprs)
        {
            _locals = _locals.Push(exprs.Aggregate(_locals.First(), (acc, p) => acc.SetItem(p.Name, p)));
            return new PopLocals(this);
        }

        class PopLocals : IDisposable
        {
            private Compiler2 _compiler;

            public PopLocals(Compiler2 c)
            {
                _compiler = c;
            }
            public void Dispose()
            {
                _compiler._locals = _compiler._locals.Pop();
            }
        }

        private IExpression CompileDo(object[] args)
        {
            return Expression.Do(args.Select(a => CompileForm(a)).ToArray());
        }
        
        private Symbol ResolveSymbol(Symbol sym)
        {
            if (sym.Namespace == null)
            {
                sym = Symbol.Intern(Namespaces.Peek(), sym.Name);
            }

            return sym;
        }
        
        private Type TypeFromSymbol(object o)
        {
            if (o is Symbol s)
            {
                if (s.Namespace != null)
                    throw new Exception($"Can't get type from {s}");

                if (s.Name == "int")
                    return typeof(int);
                throw new NotImplementedException($"No type for {s}");
            }
            throw new NotImplementedException();
        }




        private IExpression CompilePropertyLookup(Cons cons)
        {
            if (cons.Count != 2)
                throw new Exception("Only one argument can be passed to property lookups.");

            var name = ((Symbol) cons.Head).Name.Substring(2);
            var obj = cons.Tail!.Head;
            var objExpr = CompileForm(obj);

            var prop = objExpr.Type.GetProperty(name);
            if (prop != null)
                return Expression.Property(objExpr, prop);
            
            var field = objExpr.Type.GetField(name);
            if (field != null)
                return Expression.Field(objExpr, field);

            throw new Exception($"Can't find property or field {field} on {objExpr.Type}");

        }

        private IExpression CompileIf(object[] arr)
        {
            var test = arr[0];
            var then = arr[1];
            var els = arr[2];

            var testExpr = CompileForm(test);
            if (testExpr.Type != typeof(bool))
                throw new NotImplementedException($"If must be given a bool expression as a test, got {testExpr.Type}");

            var thenExpr = CompileForm(then);
            var elsExpr = CompileForm(els);
            return Expression.If(testExpr, thenExpr, elsExpr);
        }
        
        private IExpression CompileBuiltin(Symbol head, Cons? arglist)
        {
            var args = arglist == null ? Array.Empty<object>() : arglist.ToArray();
            return head.Name switch
            {
                "+" => CompileAddBuiltin(args),
                "=" => CompileEqualsBuiltin(args),
                _ => throw new NotImplementedException()
            };
        }

        private IExpression CompileEqualsBuiltin(IReadOnlyCollection<object> args)
        {
            if (args.Count == 0)
                throw new Exception("Cannot call sys/= with no arguments");
            var exprs = args.Select(CompileForm).ToArray();
            var type = exprs.First().Type;
            if (exprs.Any(a => a.Type != type))
                throw new Exception("All arguments to sys/= must be of the same type");
            IExpression? acc = null;
            foreach (var expr in exprs)
            {
                acc = acc == null ? expr : Expression.Equals(acc, expr);
            }

            return acc!;
        }


        private IExpression CompileAddBuiltin(IReadOnlyCollection<object> args)
        {
            if (args.Count == 0)
                throw new Exception("Cannot call sys/+ with no arguments");
            var exprs = args.Select(CompileForm).ToArray();
            var type = exprs.First().Type;
            if (exprs.Any(a => a.Type != type))
                throw new Exception("All arguments to sys/+ must be of the same type");
            IExpression? acc = null;
            foreach (var expr in exprs)
            {
                acc = acc == null ? expr : Expression.Add(acc, expr);
            }

            return acc!;
        }
    }
}