using System;
using System.Linq;

namespace Wyld.Expressions
{
    public class Lambda : IExpression
    {
        public Arity[] Arities { get; }

        public Lambda(string name, Arity[] arities)
        {
            Arities = arities;
            Name = name;
            var gtype = Invokable.InvokableCombination(arities.Length);
            var atypes = arities.Select(a => a.Type).OrderBy(a => a.Name);
            Type = gtype.MakeGenericType(atypes.ToArray());

        }

        public static Type GetCombinationType(Type[][] types)
        {
            var ctype = Invokable.InvokableCombination(types.Length);
            var atypes = types.Select(t => Invokable.InvokableArity(t.Length - 1).MakeGenericType(t)).OrderBy(a => a.Name);
            return ctype.MakeGenericType(atypes.ToArray());
        }

        public string Name { get; }
        public Action<WriterState>? LambdaConstructor { get; set; }

        public void Emit(WriterState state)
        {
            if (LambdaConstructor == null)
            {
                var emitter = new Emitter();
                LambdaConstructor = emitter.EmitLambda(this, state);
            }

            LambdaConstructor(state);
            state.IL.Castclass(Type);
            
        }



        public Type Type { get; }
        public This ThisParam { get; set; }
    }

    public class Arity
    {
        public Parameter[] Parameters { get; }
        public IExpression Body { get; }
        
        public Type Type { get; }

        public Arity(Parameter[] parameters, IExpression body)
        {
            Parameters = parameters;
            Body = body;

            var gtype = Invokable.InvokableArity(parameters.Length);
            Type = gtype.MakeGenericType(new [] {body.Type}.Concat(parameters.Select(p => p.Type)).ToArray());

        }

    }
}