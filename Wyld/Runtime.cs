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
                K = (Func<Effect?, object?, Result<T>>) Identity<T>
            };
            return new Result<T> {Effect = effect};
        }

        private static Result<T> Identity<T>(Effect? state, object? val)
        {
            return new() {Value = (T) val!};
        }

        public static Result<T> BuildK<T>(object kstate, object fn, Effect parent)
        {
            var result = new Result<T>
            {
                Effect = new Effect
                {
                    Parent = parent,
                    FlagValue = parent?.FlagValue,
                    Data = parent?.Data,
                    KState = kstate,
                    K = fn
                }
            };
            // Null out these fields so we can GC them if the stack sits around for awhile
            if (parent == null) return result;
            parent.FlagValue = default;
            parent.Data = null;

            return result;

        }

        public static Result<object> ResumeWith(Effect eff, object resumeData)
        {
            if (eff.Parent == null)
                return ((Func<object?, object?, Result<object>>) eff.K)(eff.KState, resumeData);

            var result = ResumeWith(eff.Parent, resumeData);
            if (result.Effect != null)
            {
                return new Result<object>
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

            dynamic obj = eff.K;
            return ((Func<object?, object?, Result<object>>) eff.K)(eff.KState, result.Value); 
            
        }
        
    }
}