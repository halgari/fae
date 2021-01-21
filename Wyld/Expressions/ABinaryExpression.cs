using System;

namespace Wyld.Expressions
{
    public abstract class ABinaryExpression : IExpression
    {
        protected ABinaryExpression(IExpression a, IExpression b)
        {
            A = a;
            B = b;
            if (A.Type != B.Type)
                throw new Exception($"Binary expression types do not match, got {a.Type} and {b.Type}");
            Type = a.Type;
        }

        public IExpression B { get; set; }

        public IExpression A { get; set; }
        
        public abstract void Emit(WriterState state);

        public Type Type { get; protected set; }
    }
}