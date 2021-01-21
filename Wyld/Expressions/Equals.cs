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
            A.Emit(state);
            B.Emit(state);
            state.IL.Ceq();
        }
    }
}