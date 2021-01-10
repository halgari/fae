using System.Collections.Generic;
using static Fae.Runtime.RuntimeObject;

namespace Fae.Runtime
{

    public static class Utils
    {
        public static object MakeList(List<object> objs)
        {
            object acc = KW.EOL;
            
            for (var idx = objs.Count - 1; idx >= 0; idx--)
            {
                acc = RT.New(KW.First, objs[idx], KW.Next, acc, KW.SizedCount, objs.Count - idx);

            }

            return acc;
        }
    }}