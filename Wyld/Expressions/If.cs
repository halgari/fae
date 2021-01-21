using System;

namespace Wyld.Expressions
{
    public class If : IExpression
    {
        public If(IExpression testExpr, IExpression thenExpr, IExpression elseExpr)
        {
            Test = testExpr;
            Then = thenExpr;
            Else = elseExpr;
            
            if (thenExpr.Type != elseExpr.Type)
                throw new Exception("Branches in If must all return the same type");
            Type = Then.Type;
        }

        public IExpression Else { get; }
        public IExpression Then { get; }
        public IExpression Test { get; }

        public void Emit(WriterState state)
        {
            var end = state.IL.DefineLabel("end");
            var branch = state.IL.DefineLabel("branch");
            Test.Emit(state);
            state.IL.Brfalse(branch);
            Then.Emit(state);
            state.IL.Br(end);
            state.IL.MarkLabel(branch);
            Else.Emit(state);
            state.IL.MarkLabel(end);
        }

        public Type Type { get; }
    }
}