using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Wyld
{
    public class Compiler
    {
        private ImmutableStack<ImmutableDictionary<string, Expression>> _locals;
        public ConcurrentDictionary<Symbol, object> Globals = new();

        public ImmutableStack<string> Namespaces = ImmutableStack<string>.Empty.Push("scratch");
        
        public Compiler()
        {
            _locals = ImmutableStack<ImmutableDictionary<string, Expression>>.Empty.Push(ImmutableDictionary<string, Expression>.Empty);
        }

        /// <summary>
        /// Compiles and executes the given form. 
        /// </summary>
        /// <returns></returns>
        public Func<object> Compile(object form)
        {
            var expr = Expression.Convert(CompileForm(form), typeof(object));
            return Expression.Lambda<Func<object>>(expr, true, ArraySegment<ParameterExpression>.Empty).Compile();
        }

        private Expression CompileForm(object? form)
        {
            return form switch
            {
                Cons c => CompileSexpr(c),
                Symbol s => CompileSymbol(s),
                null => Expression.Constant(null, typeof(object)),
                _ => Expression.Constant(form, form.GetType())
            };
        }

        private Expression CompileSymbol(Symbol symbol)
        {
            if (symbol.Namespace == null)
            {
                if (!_locals.IsEmpty && _locals.First().TryGetValue(symbol.Name, out var expr))
                    return expr;

                if (TryResolveBox(symbol, out var box))
                {
                    return Expression.Field(Expression.Constant(box, box.GetType()), box.GetType().GetField("Value")!);
                }
            }

            throw new Exception($"Undefined symbol: {symbol}");
        }

        private Expression CompileSexpr(Cons cons)
        {
            var head = cons.Head;
            switch (head)
            {
                case Symbol {Namespace: "sys"} symbol:
                    return CompileBuiltin(symbol!, cons.Tail);
                case Symbol {Namespace: null, Name: "fn"}:
                    return CompileLambda(cons.Tail!.ToArray());
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

        private Expression CompilePropertyLookup(Cons cons)
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

        private Expression CompileDefn(object[] argv)
        {
            var name = (Symbol)argv[0];
            var args = argv[1];
            var body = argv[2];

            var retType = TypeFromSymbol(((IMeta) args).Meta[KW.Type]);

            var parameters = ((Cons) args).Select(o =>
            {
                var s = (Symbol) o;
                if (s.Namespace != null)
                    throw new Exception("arg names must be simple");
                return Expression.Parameter(TypeFromSymbol(s.Meta[KW.Type]), s.Name);
            }).ToArray();

            var boxType = GenericFuncForArity(parameters.Length).MakeGenericType(parameters.Select(p => p.Type).Concat(new[] {retType}).ToArray());

            var box = DefineBox(name, boxType);

            using var _ = WithLocals(parameters);
            var bodyExpr = CompileForm(body);
            var fn = Expression.Lambda(bodyExpr, true, parameters);


            return Expression.Assign(Expression.Field(Expression.Constant(box, box.GetType()), box.GetType().GetField("Value")!), fn);
        }

        public Type GenericFuncForArity(int arity)
        {
            switch (arity)
            {
                case 0:
                    return typeof(Func<>);
                case 1:
                    return typeof(Func<,>);
                case 2:
                    return typeof(Func<,,>);
            }

            throw new NotImplementedException();
        }

        private Expression CompileIf(object[] arr)
        {
            var test = arr[0];
            var then = arr[1];
            var els = arr[2];

            var testExpr = CompileForm(test);
            if (testExpr.Type != typeof(bool))
                testExpr = Expression.NotEqual(testExpr, Expression.Constant(null, testExpr.Type));

            var thenExpr = CompileForm(then);
            var elsExpr = CompileForm(els);
            return Expression.Condition(testExpr, thenExpr, elsExpr);
        }

        private Expression CompileDef(object[] args)
        {
            var sym = (Symbol)args[0];
            var boxType = TypeFromSymbol(sym.Meta[KW.Type]);
            var val = args[1];
            var box = DefineBox(sym, boxType);
            var valExpr = CompileForm(val);
            return Expression.Assign(Expression.Field(Expression.Constant(box, box.GetType()), box.GetType().GetField("Value")!), valExpr);
        }

        private object DefineBox(Symbol sym, Type t)
        {
            sym = ResolveSymbol(sym);

            if (Globals.TryGetValue(sym, out var rbox))
            {
                return rbox;
            }

            var box = Activator.CreateInstance(typeof(Box<>).MakeGenericType(t));
            if (Globals.TryAdd(sym, box))
            {
                return Globals[sym];
            }

            return box;
        }

        private bool TryResolveBox(Symbol sym, out object box)
        {
            return Globals.TryGetValue(ResolveSymbol(sym), out box);
        }

        private Symbol ResolveSymbol(Symbol sym)
        {
            if (sym.Namespace == null)
            {
                sym = Symbol.Intern(Namespaces.Peek(), sym.Name);
            }

            return sym;
        }

        private Expression CompileLambda(object[] vals)
        {
            if (vals.Length != 2)
                throw new Exception("Expected 2 args to fn");

            var retType = TypeFromSymbol(((IMeta) vals[0]).Meta[KW.Type]);

            var parameters = ((Cons) vals[0]).Select(o =>
            {
                var s = (Symbol) o;
                if (s.Namespace != null)
                    throw new Exception("arg names must be simple");
                return Expression.Parameter(TypeFromSymbol(s.Meta[KW.Type]), s.Name);
            }).ToArray();

            using var _ = WithLocals(parameters);
            var body = CompileForm(vals[1]);


            return Expression.Lambda(body, parameters);
        }

        private IDisposable WithLocals(params ParameterExpression[] exprs)
        {
            _locals = _locals.Push(exprs.Aggregate(_locals.First(), (acc, p) => acc.Add(p.Name, p)));
            return new PopLocals(this);
        }

        class PopLocals : IDisposable
        {
            private Compiler _compiler;

            public PopLocals(Compiler c)
            {
                _compiler = c;
            }
            public void Dispose()
            {
                _compiler._locals = _compiler._locals.Pop();
            }
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

        private Expression CompileBuiltin(Symbol head, Cons? arglist)
        {
            var args = arglist == null ? Array.Empty<object>() : arglist.ToArray();
            return head.Name switch
            {
                "+" => CompileAddBuiltin(args),
                "=" => CompileEqualsBuiltin(args),
                _ => throw new NotImplementedException()
            };
        }

        private Expression CompileEqualsBuiltin(IReadOnlyCollection<object> args)
        {
            if (args.Count == 0)
                throw new Exception("Cannot call sys/= with no arguments");
            var exprs = args.Select(CompileForm).ToArray();
            var type = exprs.First().Type;
            if (exprs.Any(a => a.Type != type))
                throw new Exception("All arguments to sys/= must be of the same type");
            Expression? acc = null;
            foreach (var expr in exprs)
            {
                acc = acc == null ? expr : Expression.Equal(acc, expr);
            }

            return acc!;
        }


        private Expression CompileAddBuiltin(IReadOnlyCollection<object> args)
        {
            if (args.Count == 0)
                throw new Exception("Cannot call sys/+ with no arguments");
            var exprs = args.Select(CompileForm).ToArray();
            var type = exprs.First().Type;
            if (exprs.Any(a => a.Type != type))
                throw new Exception("All arguments to sys/+ must be of the same type");
            Expression? acc = null;
            foreach (var expr in exprs)
            {
                acc = acc == null ? expr : Expression.Add(acc, expr);
            }

            return acc!;
        }
    }
}