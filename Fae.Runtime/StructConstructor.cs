using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Fae.Runtime
{
    public class StructConstructor
    {
        public static Type GetStructValueType(Type t)
        {
            if (t == typeof(int))
                return t;

            return typeof(object);
        }

        private static ConcurrentDictionary<IdentityKey, Type> _structRegistry = new();
        public static Type MakeStruct((Keyword keyword, Type type)[] members)
        {
            var key = MakeKey(members);

            if (_structRegistry.TryGetValue(key, out var found))
                return found;

            lock (_structRegistry)
            {
                if (_structRegistry.TryGetValue(key, out found))
                    return found;
                
                var aName = new AssemblyName("ConstructedStruct");
                var ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndCollect);
                var mb = ab.DefineDynamicModule(aName.Name!);
                var typename = "Struct_" + key.GetHashCode();
                var tb = mb.DefineType(typename, TypeAttributes.Public | TypeAttributes.Sealed);

                var fields = new List<(Keyword keyword, Type type, FieldBuilder field)>();
                foreach (var member in members)
                {
                    var field = tb.DefineField(
                        member.keyword.ToString().Replace("/", "_").Replace(":", "_").Replace(".", "_"),
                        member.type,
                        FieldAttributes.Public
                    );
                    field.SetCustomAttribute(new CustomAttributeBuilder(
                        typeof(StructMemberAttribute).GetConstructors().First(), new[] {member.keyword.GetStr()}));
                    fields.Add((member.keyword, member.type, field));
                }

                var parameterTypes = fields.Select(t => t.type).ToArray();

                var ctor1 = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
                var ctor1IL = ctor1.GetILGenerator();

                foreach (var (param, idx) in fields.Select((f, idx) => (f, idx)))
                {
                    ctor1IL.Emit(OpCodes.Ldarg_0);
                    ctor1IL.Emit(OpCodes.Ldarg, idx + 1);
                    ctor1IL.Emit(OpCodes.Stfld, param.field);
                }

                ctor1IL.Emit(OpCodes.Ret);


                var type = tb.CreateType();
                DynamicRuntime.StructDefinitions.TryAdd(type,
                    new ReflectiveStructDefinition(type,
                        fields.Select(f => (f.keyword, f.type, f.field.Name)).ToArray()));

                _structRegistry.TryAdd(key, type);

                return type;
            }
        }

        private static IdentityKey MakeKey((Keyword keyword, Type type)[] members)
        {
            return new(members.SelectMany(m => new object[] {m.keyword, m.type}).ToArray());
        }
    }

    internal class ReflectiveStructDefinition : IStructDefinition
    {
        private (Keyword keyword, Type type)[] _members;
        private Dictionary<Keyword, (Keyword keyword, Type type, string name)> _definitions;
        private Type _baseType;

        public ReflectiveStructDefinition(Type baseType, (Keyword keyword, Type type, string name)[] defs)
        {
            _baseType = baseType;
            _definitions = defs.ToDictionary(d => d.keyword);
            _members = defs.Select(def => (def.keyword, def.type)).ToArray();
        }
        
        public (Keyword, Type)[] GetMembers()
        {
            return _members;
        }

        public Expression MemberGetter(Expression self, Keyword memberName)
        {
            if (_definitions.TryGetValue(memberName, out var member))
            {
                return Expression.Convert(Expression.Field(Expression.Convert(self, _baseType), member.name), typeof(object));
            }

            throw new NotImplementedException();
        }
    }
}