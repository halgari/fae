using System;
using System.Linq;

namespace Wyld.Expressions
{
    public class Do : IExpression
    {
        public Do(IExpression[] body)
        {
            Body = body;
            Type = body.Last().Type;
        }

        public IExpression[] Body { get; set; }

        public void Emit(WriterState state)
        {
            using (var _ = state.WithTailCallFlag(false))
            {
                foreach (var expr in Body.SkipLast(1))
                {
                    expr.Emit(state);
                    state.IL.Pop();
                }
            }

            Body.Last().Emit(state);
        }

        public Type Type { get; }
    }
}