using System;

namespace Wyld.Expressions
{
    public class NullConstant : IExpression
    {
        public Type Type { get; }
        public NullConstant(Type tp)
        {
            Type = tp;
        }

        public void Emit(WriterState state)
        {
            throw new NotImplementedException();
        }
    }
}