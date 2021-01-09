using System;
using System.Linq.Expressions;

namespace Fae.Runtime
{
    public interface IStructDefinition
    {
        public (Keyword, Type)[] GetMembers();
        public Expression MemberGetter(Expression self, Keyword memberName);
    }
}