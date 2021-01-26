namespace Wyld.Expressions
{
    public class Equals : ABinaryExpression
    {
        public Equals(IExpression a, IExpression b) : base(a, b)
        {
            Type = typeof(bool);
        }

        public override void Emit(WriterState state)
        { 
            using var _ = state.WithTailCallFlag(false);
            A.Emit(state);
            state.PushToEvalStack(A.Type);
            B.Emit(state);
            state.IL.Ceq();
            state.PopFromEvalStack();
        }
    }
}