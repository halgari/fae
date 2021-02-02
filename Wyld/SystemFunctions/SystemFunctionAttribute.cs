using System;

namespace Wyld.SystemFunctions
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SystemFunctionAttribute : Attribute
    {
        public SystemFunctionAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}