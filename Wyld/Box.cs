using System;

namespace Wyld
{
    public interface IBox
    {
        Type Type { get; }
        Symbol Name { get; }
    }
    public class Box<T> : IBox
    {
        public Type Type => typeof(T);
        public T Value;
        public Symbol Name { get; }
        public Box(Symbol name)
        {
            Name = name;
        }

    }
}