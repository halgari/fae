namespace Wyld.Expressions
{
    public class Add : ABinaryExpression
    {
        public Add(IExpression a, IExpression b) : base(a, b)
        {
        }

        public override void Emit(WriterState state)
        {
            A.Emit(state);
            B.Emit(state);
            state.IL.Add();
        }
    }
}