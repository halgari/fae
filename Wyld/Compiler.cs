using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Wyld
{
    public class Compiler
    {
        private ImmutableStack<ImmutableDictionary<string, Expression>> _locals;

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
            }
            throw new NotImplementedException();
        }

        private Expression CompileSexpr(Cons cons)
        {
            var head = cons.Head;
            if (head is Symbol {Namespace: "sys"} symbol)
                return CompileBuiltin(symbol!, cons.Tail);
            if (head is Symbol {Namespace: null, Name: "fn"})
                return CompileLambda(cons.Tail!.ToArray());

            var fn = CompileForm(head);
            var args = cons.Tail!.Select(CompileForm).ToArray();
            return Expression.Invoke(fn, args);
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
                throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        private Expression CompileBuiltin(Symbol head, Cons? arglist)
        {
            var args = arglist == null ? Array.Empty<object>() : arglist.ToArray();
            return head.Name switch
            {
                "+" => CompileAddBuiltin(args),
                "fn" when head.Namespace == null => CompileFn(args),
                _ => throw new NotImplementedException()
            };
        }

        private Expression CompileFn(object[] args)
        {
            throw new NotImplementedException();
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