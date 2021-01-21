using System;

namespace Wyld.Expressions
{
    public class AssignToField : IExpression
    {
        public Field Field { get; }
        public IExpression Value { get; }

        public AssignToField(Field field, IExpression value)
        {
            Value = value;
            Field = field;
            Type = value.Type;
        }
        
        
        public void Emit(WriterState state)
        {
            Field.Source.Emit(state);
            Value.Emit(state);
            state.IL.Stfld(Field.FieldInfo);
            Field.Emit(state);
        }

        public Type Type { get; }
    }
}