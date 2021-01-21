using System;

namespace Wyld.Expressions
{
    public class This : Parameter
    {
        public This(string name, Type type) : base(name, type, -1)
        {
            
        }
    }
}