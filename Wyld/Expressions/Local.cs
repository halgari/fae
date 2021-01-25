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
            if (state.EmittingInvokeK && state.LocalRemaps.TryGetValue(this, out var remap))
            {
                state.IL.Ldloc(remap);
                return;
            }

            _iLLocal ??= state.IL.DeclareLocal(Type, Name);
            state.IL.Ldloc(_iLLocal);
        }

        public Type Type { get; }
        public string Name { get; }

        private GroboIL.Local? _iLLocal;

        public void EmitBind(WriterState state)
        {
            if (state.EmittingInvokeK && state.LocalRemaps.TryGetValue(this, out var remap))
                throw new Exception("Can't remap a previously unbound K in a InvokeK");
            _iLLocal = state.IL.DeclareLocal(Type, Name);
            state.IL.Stloc(_iLLocal);
        }
    }
}