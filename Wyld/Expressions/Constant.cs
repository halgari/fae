using System;
using System.Reflection;

namespace Wyld.Expressions
{
    public class Constant : IExpression
    {
        public Constant(object c)
        {
            Value = c;
            Type = c.GetType();
        }
        
        public Constant(object c, Type valueType)
        {
            Value = c;
            Type = valueType;
            if (!valueType.IsInstanceOfType(c))
                throw new Exception($"{c.GetType().FullName} cannot be assigned to {valueType.FullName}");
        }

        public object Value { get; }
        public Type Type { get; }

        public void Emit(WriterState state)
        {
            switch (Value)
            {
                case int ivalue:
                    state.IL.Ldc_I4(ivalue);
                    return;
                case long lvalue:
                    state.IL.Ldc_I8(lvalue);
                    return;
                case string svalue:
                    state.IL.Ldstr(svalue);
                    return;
                case IBox box:
                    state.IL.Ldfld(state.Emitter.AddNonNativeConstant(box));
                    return;
                case Keyword kw:
                    state.IL.Ldfld(state.Emitter.AddNonNativeConstant(kw));
                    return;
            }

            throw new NotImplementedException($"Can't emit constant for {Value} of type {Value.GetType()}");
        }
    }
}