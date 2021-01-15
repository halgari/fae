using System.Runtime.CompilerServices;

namespace Wyld
{
    public interface IEffect
    {
    
    }
    
    public struct Result<T>
    {
        public readonly T Value;
        public readonly IEffect? Effect;

        public Result(T value)
        {
            Value = value;
            Effect = null;
        }

        public Result(IEffect effect)
        {
            Value = default;
            Effect = effect;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEffect() => Effect != null;
        
    }
}