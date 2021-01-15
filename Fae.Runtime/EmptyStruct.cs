using System;
using System.Linq.Expressions;

namespace Fae.Runtime
{
    public class EmptyStruct
    {
    }

    public class EmptyStructTypeDefinition : IStructDefinition
    {
        public (Keyword, Type)[] GetMembers()
        {
            return Array.Empty<(Keyword, Type)>();
        }

        public Expression MemberGetter(Expression self, Keyword memberName)
        {
            throw new NotImplementedException();
        }
    }
}