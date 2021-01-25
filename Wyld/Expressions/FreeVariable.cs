using System;
using System.Reflection;
using System.Threading;

namespace Wyld.Expressions
{
    /// <summary>
    /// Identifies a variable that is sourced from outside of a closure. Calling Emit on this instance
    /// will mark the closure as being "in use" and the emitter will generate a lambda with constructor
    /// slots for holding these variables;
    /// </summary>
    public class FreeVariable : ILocal
    {
        public FreeVariable(ILocal source)
        {
            Source = source;
        }
        public FieldInfo? FieldInfo { get; set; } // Will be filled in by the emitter.

        public ILocal Source { get; }
        public bool InUse { get; private set; } = false;

        public void Emit(WriterState state)
        {
            InUse = true;
            if (state.EmittingInvokeK && state.LocalRemaps.TryGetValue(this, out var newloc))
            {
                state.IL.Ldloc(newloc);
                return;
            }


            state.IL.Ldarg(0);
            var field = state.Emitter.GetFreeVariableField(this);
            state.IL.Ldfld(field);
        }

        public Type Type => Source.Type;
        public string Name => Source.Name;
    }
}