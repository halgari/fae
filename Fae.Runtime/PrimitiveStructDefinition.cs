using System;
using System.Linq.Expressions;

namespace Fae.Runtime
{
    public class PrimitiveStructDefinition<T> : IStructDefinition
    {
        private Keyword _valueMember;
        private (Keyword _valueMember, Type)[] _members;

        public PrimitiveStructDefinition(string valueName)
        {
            _valueMember = Keyword.Intern(valueName);
            _members = new[]
            {
                (_valueMember, typeof(T))
            };
        }

        public (Keyword, Type)[] GetMembers()
        {
            return _members;
        }

        public Expression MemberGetter(Expression self, Keyword memberName)
        {
            if (ReferenceEquals(memberName, _valueMember))
                return self;
            throw new NotImplementedException();
        }
    }
}