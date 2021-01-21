using System;
using System.Collections.Concurrent;

namespace Wyld
{
    public static class Environment
    {
        private static ConcurrentDictionary<Symbol, IBox> Globals = new();
        
        public static IBox DefineBox(Symbol sym, Type t)
        {
            if (Globals.TryGetValue(sym, out var rbox))
            {
                return rbox;
            }

            var box = (IBox)Activator.CreateInstance(typeof(Box<>).MakeGenericType(t), sym)!;
            if (Globals.TryAdd(sym, box))
            {
                return Globals[sym];
            }

            return box;
        }

        public static bool TryResolveBox(Symbol sym, out IBox box)
        {
            return Globals.TryGetValue(sym, out box);
        }


    }
}