using System;

namespace Wyld.Expressions
{
    public class Parameter : IExpression
    {
        public Parameter(string name, Type type, int idx)
        {
            Name = name;
            Type = type;
            Idx = idx;
        }

        public int Idx { get; set; }

        public string Name { get; }

        public void Emit(WriterState state)
        {
            state.IL.Ldarg(Idx + 1);
        }

        public Type Type { get; }
    }
}