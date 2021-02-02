using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Wyld
{
    public static class Environment
    {
        private static ConcurrentDictionary<Symbol, IBox> Globals = new();
        private static Dictionary<Keyword, Namespace> Namespaces = new();

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
        
        public static void Reset()
        {
            Globals.Clear();
            Keyword.MetadataRegistry.Clear();
            lock (Namespaces)
            {
                Namespaces.Clear();
                Namespaces.Add(Consts.DefaultNamespaceName, new Namespace(Consts.DefaultNamespaceName)
                {
                    FullyInScope = new [] {Consts.SystemNamespaceName}
                });
            }
        }

        public static Keyword[] GetFullyInScope(Keyword ns)
        {
            lock (Namespaces)
            {
                if (Namespaces.TryGetValue(ns, out var resolved))
                {
                    return resolved.FullyInScope;
                }

                return Array.Empty<Keyword>();
            }
        }
    }
}