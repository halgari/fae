using System;

namespace Wyld.Expressions
{
    public abstract class Invoke : IExpression
    {
        public IExpression Fn { get; }
        public IExpression[] Args { get; }

        public Invoke(IExpression fn, IExpression[] args)
        {
            Fn = fn;
            Args = args;
        }

        public abstract void Emit(WriterState state);

        public Type Type { get; protected set; }
    }
}