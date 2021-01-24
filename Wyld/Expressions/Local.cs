using System;
using GrEmit;

namespace Wyld.Expressions
{
    public class Local : ILocal
    {
        public Local(string name, Type type)
        {
            Name = name;
            Type = type;
        }
        public void Emit(WriterState state)
        {
            _iLLocal ??= state.IL.DeclareLocal(Type, Name);
            state.IL.Ldloc(_iLLocal);
        }

        public Type Type { get; }
        public string Name { get; }

        private GroboIL.Local? _iLLocal;

        public void EmitBind(WriterState state)
        {
            _iLLocal = state.IL.DeclareLocal(Type, Name);
            state.IL.Stloc(_iLLocal);
        }
    }
}