using System;
using System.Collections.Immutable;
using GrEmit;

namespace Wyld.Expressions
{
    public class WriterState
    {
        public WriterState(GroboIL il, Emitter emitter)
        {
            IL = il;
            Emitter = emitter;
            _tailCallFlags = ImmutableStack<bool>.Empty.Push(true);
        }

        private ImmutableStack<bool> _tailCallFlags;

        public IDisposable WithTailCallFlag(bool flag)
        {
            _tailCallFlags = _tailCallFlags.Push(flag);
            return new TailCallFlagPopper(this);
        }

        public bool CanTailCall => _tailCallFlags.Peek();

        public Emitter Emitter { get; }
        public GroboIL IL { get;  }
        
        private class TailCallFlagPopper : IDisposable
        {
            private WriterState _state;

            public TailCallFlagPopper(WriterState writerState)
            {
                _state = writerState;
            }

            public void Dispose()
            {
                _state._tailCallFlags = _state._tailCallFlags.Pop();
            }
        }


        public void EmitResultPostlude(Type type)
        {
            if (CanTailCall)
            {
                IL.Ret();
                return;
            }

            var tp = typeof(Result<>).MakeGenericType(type);
            var tmp = IL.DeclareLocal(tp);
            IL.Stloc(tmp);
            IL.Ldloca(tmp);
            IL.Ldfld(tp.GetField("Value"));
        }
    }


}