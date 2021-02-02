using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Wyld.SystemFunctions;

namespace Wyld.Expressions
{
    public class DynamicCallSite : Invoke
    {
        public DynamicCallSite(IExpression fn, IExpression[] args) : base(fn, args)
        {
            var tp = fn.Type;
            while (true)
            {
                if (tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(ADynamicDispatchSystemFunction<>))
                {
                    Type = tp.GetGenericArguments().First();
                    break;
                }

                if (tp.BaseType == null)
                    throw new Exception($"Dynamic callsite must call a dynamic function, got {fn.Type}");
                tp = tp.BaseType!;
            }

        }

        public override void Emit(WriterState state)
        {
            var delegateType = Invokable.Action(Args.Length + 1).MakeGenericType(new []{typeof(CallSite)}.Concat(Args.Select(a => a.Type)).ToArray());
            var fi = state.Emitter.EmitCallSite(delegateType);
            state.IL.Ldfld(fi);
            state.IL.Ldfld(fi.FieldType.GetField("Target"));
            state.IL.Ldfld(fi);

            foreach (var arg in Args)
            {
                arg.Emit(state);
            }
            state.IL.Call(delegateType.GetMethod("Invoke"));
        }
    }
}