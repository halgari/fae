using System;

namespace Wyld.Expressions
{
    public class Convert<T> : IExpression
    {
        public Convert(IExpression expr)
        {
            Type = typeof(T);
            Expression = expr;
        }
        public void Emit(WriterState state)
        {
            Expression.Emit(state);
            if (Type == typeof(object) && Expression.Type.IsValueType)
            {
                state.IL.Box(Expression.Type);
                return;
            }

            if (!Type.IsValueType && !Expression.Type.IsValueType && Expression.Type.IsSubclassOf(Type))
            {
                state.IL.Castclass(Type);
                return;
            }
                

            throw new NotImplementedException($"Can't convert from {Expression.Type} to {Type}");
        }
        

        public Type Type { get; }
        public IExpression Expression { get; }
    }
}