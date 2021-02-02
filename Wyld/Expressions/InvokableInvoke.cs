using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Wyld.Expressions
{
    public class InvokableInvoke : Invoke
    {
        public MethodInfo MethodInfo { get; }

        public InvokableInvoke(IExpression fn, IExpression[] args) : base(fn, args)
        {
            var invokingMethods = InvokableMethodsFrom(fn.Type)
                .Where(m => m.GetParameters().Length == args.Length)
                .Where(m => m.GetParameters().Select(p => p.ParameterType).ToArray().SequenceEqual(args.Select(a => a.Type).ToArray()))
                .ToArray();
            if (invokingMethods.Length == 0)
                throw new Exception($"No method found");
            if (invokingMethods.Length > 1)
                throw new Exception("More than one method found");

            MethodInfo = invokingMethods[0];

            DeclaringType = MethodInfo.DeclaringType!;
            Type = DeclaringType!.GetGenericArguments()[0];

        }

        public static IEnumerable<MethodInfo> InvokableMethodsFrom(Type tp)
        {
            if (tp.Name.StartsWith("IInvokableCombination`"))
                return tp.GetGenericArguments()
                    .SelectMany(t => t.GetMethods().Where(m => m.Name == "Invoke"));

            return tp
                .GetInterfaces()
                .First(i => i.Name.StartsWith("IInvokableCombination`"))
                .GetGenericArguments()
                .SelectMany(t => t.GetMethods().Where(m => m.Name == "Invoke"));
        }

        public Type DeclaringType { get; }

        public override void Emit(WriterState state)
        {
            Fn.Emit(state);
            state.PushToEvalStack(Fn.Type);
            state.IL.Castclass(DeclaringType);
            state.EmitEvalArgs(Args);

            state.IL.Call(MethodInfo, tailcall: state.CanTailCall);
            state.PopFromEvalStack(Args.Length + 1);

            state.EmitResultPostlude(Type, this);
        }
    }
}