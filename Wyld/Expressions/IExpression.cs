using System;

namespace Wyld.Expressions
{
    public interface IExpression
    {
        void Emit(WriterState state);
        
        Type Type { get; }
    }
}