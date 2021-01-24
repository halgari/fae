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

        public static Result<T> BuildK<T>(object data, Func<object?, object?, Result<T>> fn, Effect parent)
        {
            var result = new Result<T>
            {
                Effect = new Effect
                {
                    Parent = parent,
                    FlagValue = parent?.FlagValue,
                    Data = parent?.Data,
                    KState = data,
                    K = fn
                }
            };
            // Null out these fields so we can GC them if the stack sits around for awhile
            if (parent == null) return result;
            parent.FlagValue = default;
            parent.Data = null;

            return result;

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