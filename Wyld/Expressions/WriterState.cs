using GrEmit;

namespace Wyld.Expressions
{
    public class WriterState
    {
        public WriterState(GroboIL il, Emitter emitter)
        {
            IL = il;
            Emitter = emitter;
        }

        public Emitter Emitter { get; }
        public GroboIL IL { get;  }
    }
}