using System;

namespace Wyld
{
    public static class Runtime
    {
        public static Result<T> Raise<T>(object flag, object data)
        {
            var effect = new Effect
            {
                Parent = null,
                FlagValue = flag,
                KState = null,
                Data = data,
                K = (Func<object?, object?, Result<T>>) Identity<T>
            };
            return new Result<T> {Effect = effect};
        }

        private static Result<T> Identity<T>(object? state, object? val)
        {
            return new() {Value = (T) val!};
        }

        public static Result<T> ResumeWith<T>(Effect eff, object resumeData)
        {
            if (eff.Parent == null)
                return ((Func<object?, object?, Result<T>>) eff.K)(eff.KState, resumeData);

            var result = ResumeWith<T>(eff.Parent, resumeData);
            if (result.Effect != null)
            {
                return new Result<T>
                {
                    Effect = new Effect
                    {
                        Parent = result.Effect,
                        FlagValue = result.Effect.FlagValue,
                        KState = eff.KState,
                        Data = result.Effect.Data,
                        K = eff.K,
                    }
                };
            }
            return ((Func<object?, object?, Result<T>>) eff.K)(eff.KState, result.Value); 
            
        }
        
    }
}