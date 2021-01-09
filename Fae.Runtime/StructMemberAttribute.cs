using System;
using System.Linq;
using System.Reflection;

namespace Fae.Runtime
{
    public class StructMemberAttribute : Attribute
    {
        private string _name;

        public StructMemberAttribute(string name)
        {
            _name = name;
        }

        public static IStructDefinition GetDefinition(Type src)
        {
            var members = src.GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Select(m => new {member =m, attribute=m.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(StructMemberAttribute))})
                .Where(m => m.attribute != null)
                .Select(m => new {member = m.member, attribute = (string)m.attribute.ConstructorArguments.First().Value})
                .OrderBy(m => m.attribute)
                .Select(m => new {m.member, m.attribute, keyword = Keyword.Intern(m.attribute)})
                .ToArray();

            var memberList = members.Select(m => (m.keyword, GetReturnType(m.member)))
                .ToArray();

            return null;
        }

        private static Type GetReturnType(MemberInfo member)
        {


            throw new NotImplementedException();
        }
    }
}