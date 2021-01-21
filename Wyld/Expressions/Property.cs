using System;
using System.Reflection;

namespace Wyld.Expressions
{
    public class Property : IExpression
    {
        public Property(IExpression source, PropertyInfo prop)
        {
            Prop = prop;
            Source = source;
            Type = Prop.PropertyType;
        }

        public IExpression Source { get; }

        public PropertyInfo Prop { get; }

        public void Emit(WriterState state)
        {
            Source.Emit(state);
            state.IL.Call(Prop.GetMethod!);
        }

        public Type Type { get; }
    }
}