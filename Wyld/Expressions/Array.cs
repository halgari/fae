using System;
using System.Linq;

namespace Wyld.Expressions
{
    public class MakeArray : IExpression
    {
        public MakeArray(IExpression[] values)
        {
            if (values.Any(v => v.Type != values[0].Type))
                throw new Exception("All arguments to Array must be of the same type");
            Values = values;
            Type = values[0].Type.MakeArrayType();
        }

        public IExpression[] Values { get; }

        public void Emit(WriterState state)
        {
            var elementType = Values[0].Type;
            using var _ = state.WithTailCallFlag(false);
            foreach (var value in Values)
            {
                value.Emit(state);
                state.PushToEvalStack(value.Type);
            }
            state.IL.Ldc_I4(Values.Length);
            state.IL.Newarr(Values[0].Type);
            state.PushToEvalStack(Type);
            foreach (var (val, idx) in Values.Select((val, idx) => (val, idx)))
            {
                state.IL.Dup();
                state.IL.Ldc_I4(idx);
                val.Emit(state);
                state.IL.Stelem(elementType);
            }
        }

        public Type Type { get; }
    }
}