using System;
using System.Reflection;

namespace Wyld.Expressions
{
    public class Field : IExpression
    {
        public Field(IExpression source, FieldInfo field)
        {
            Source = source;
            FieldInfo = field;
            Type = field.FieldType;
        }

        public IExpression Source { get; }
        public FieldInfo FieldInfo { get; }
        public void Emit(WriterState state)
        {
            Source.Emit(state);
            state.IL.Ldfld(FieldInfo);
        }

        public Type Type { get; }
    }
}