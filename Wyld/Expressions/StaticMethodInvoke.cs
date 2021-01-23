using System;
using System.Reflection;

namespace Wyld.Expressions
{
    public class StaticMethodInvoke : IExpression
    {
        public StaticMethodInvoke(MethodInfo method, IExpression[] args)
        {
            Method = method;
            Arguments = args;
            
            var retType = Method.ReturnType;
            if (retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                Type = retType.GetGenericArguments()[0];
            }
            else
            {
                Type = retType;
            }
        }

        public IExpression[] Arguments { get; }
        public MethodInfo Method { get; }

        public void Emit(WriterState state)
        {
            using var _ = state.WithTailCallFlag(false);
            foreach (var expr in Arguments)
                expr.Emit(state);
            state.IL.Call(Method);
            state.EmitResultPostlude(Type);
        }

        public Type Type { get; }
    }
}