using System;

namespace Wyld.SystemFunctions
{
    [SystemFunction("wyld.system/set-keyword-type")]
    public class SetKeywordType : IInvokableArity<Keyword, Keyword, Type>, IInvokableCombination<IInvokableArity<Keyword, Keyword, Type>>
    {
        public Result<Keyword> Invoke(Keyword kw, Type type)
        {
            var metadata = new Keyword.KeywordMetadata {Keyword = kw, Type = type};
            if (Keyword.MetadataRegistry.TryAdd(kw, metadata))
                return new Result<Keyword> {Value = kw};

            return Helpers.UnrecoverableError<Keyword>($"Keyword {kw} already has metadata");
        }
    }

}