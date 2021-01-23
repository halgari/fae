using System;

namespace Wyld.Expressions
{
    public class Let : IExpression
    {
        public Let(Local local, IExpression bind, IExpression body)
        {
            Body = body;
            Bind = bind;
            Local = local;
            Type = body.Type;
        }

        public IExpression Bind { get; }
        public Local Local { get; }

        public IExpression Body { get; }

        public void Emit(WriterState state)
        {
            Bind.Emit(state);
            using (var _ = state.WithTailCallFlag(false))
            {
                Local.EmitBind(state);
            }

            Body.Emit(state);
        }

        public Type Type { get; }
    }
}