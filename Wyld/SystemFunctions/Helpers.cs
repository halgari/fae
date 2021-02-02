using System;
using System.Linq;

namespace Wyld.SystemFunctions
{
    public static class Helpers
    {

        public static void RegisterAll()
        {
            DefineFunction<SetKeywordType>();
            DefineFunction<NewStruct>();

            DefineSystemType<int>("int");
        }

        private static void DefineSystemType<T>(string typeName)
        {
            var box = (Box<Type>)Environment.DefineBox(Symbol.Intern(Consts.SystemNamespaceName.Name, typeName), typeof(Type));
            box.Value = typeof(T);
        }

        private static void DefineFunction<T>()
        {
            var nm = typeof(T).GetCustomAttributes(typeof(SystemFunctionAttribute), true)
                .OfType<SystemFunctionAttribute>()
                .First()
                .Name;

            var cls = Activator.CreateInstance<T>();

            var box = (Box<T>)Environment.DefineBox(Symbol.Parse(nm), typeof(T));
            box.Value = cls;
        }
        public static Result<T> UnrecoverableError<T>(string msg)
        {
            var err = new Exception(msg);
            return new Result<T>
            {
                Effect = new Effect
                {
                    FlagValue = err,
                    Data = null,
                    KState = err,
                    K = (Func<Effect, object, Result<T>>) Continue<T>
                }
            };
        }

        private static Result<T> Continue<T>(Effect e, object cData)
        {
            return new()
            {
                Effect = new Effect
                {
                    FlagValue = e.Data,
                    Data = null,
                    KState = e.Data,
                    K = (Func<Effect, object, Result<T>>)Continue<T>
                }
            };
        }
    }
}