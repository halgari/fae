using System;

namespace Wyld.Expressions
{
    public class Parameter : ILocal
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
            if (state.EmittingInvokeK && state.LocalRemaps.TryGetValue(this, out var newloc))
                state.IL.Ldloc(newloc);
            else 
                state.IL.Ldarg(Idx + 1);
        }

        public Type Type { get; }
    }
}