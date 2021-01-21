using System;

namespace Wyld
{
    /// <summary>
    /// Represents either a value or an effect
    /// </summary>
    public struct Result<T>
    {
        public T Value;
        public AEffect Effect;

        public Result<TR> WithK<TR>(Func<object, TR> k)
        {
            return default;
        }
    }
    
    public abstract class AEffect
    {
        public Func<object, object> K;

    }

}