using System;
using System.Collections.Generic;

namespace Wyld
{
    public class Namespace
    {
        public Namespace(Keyword name)
        {
            Name = name;
            
        }
        public Keyword Name { get; }
        public Keyword[] FullyInScope = Array.Empty<Keyword>();
    }
}