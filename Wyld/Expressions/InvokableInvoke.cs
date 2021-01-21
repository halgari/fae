using System;
using System.Linq;
using System.Reflection;

namespace Wyld.Expressions
{
    public class InvokableInvoke : Invoke
    {
        public MethodInfo MethodInfo { get; }

        public InvokableInvoke(IExpression fn, IExpression[] args) : base(fn, args)
        {
            var invokingMethods = fn.Type.GetGenericArguments()
                .SelectMany(t => t.GetMethods().Where(m => m.Name == "Invoke"))
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

        public Type DeclaringType { get; }

        public override void Emit(WriterState state)
        {
            Fn.Emit(state);
            state.IL.Castclass(DeclaringType);
            foreach(var arg in Args)
                arg.Emit(state);
            state.IL.Call(MethodInfo);

            
        }
    }
}