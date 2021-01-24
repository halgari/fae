using System;
using System.Data;

namespace Wyld
{
    /// <summary>
    /// Represents either a value or an effect
    /// </summary>
    public struct Result<T>
    {
        public T Value;
        public Effect? Effect;
    }
    
    public class Effect
    {
        /// <summary>
        /// Next effect up the chain
        /// </summary>
        public Effect? Parent;
     
        /// <summary>
        /// Arbitrary state handed to K
        /// </summary>
        public object? KState;
        
        /// <summary>
        /// A function that takes State and a param and continues execution
        /// </summary>
        public object K;

        /// <summary>
        /// Actual effect data
        /// </summary>
        public object? Data;

        /// <summary>
        /// Handlers should look at this value when considering if they should handle the effect
        /// </summary>
        public object? FlagValue;

    }

}