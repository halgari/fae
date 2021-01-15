using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using BindingFlags = System.Reflection.BindingFlags;

namespace Fae.Runtime
{
    public class RuntimeObject : IDynamicMetaObjectProvider
    {
        public static dynamic RT = new RuntimeObject();
        
        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DynamicRuntime(parameter, BindingRestrictions.GetTypeRestriction(parameter, typeof(RuntimeObject)), this);
        }
    }

    public class DynamicRuntime : DynamicMetaObject
    {
        public static ConcurrentDictionary<Type, IStructDefinition> StructDefinitions = new();

        static DynamicRuntime()
        {
            StructDefinitions.TryAdd(typeof(int), new PrimitiveStructDefinition<int>("fae.int/value"));
            StructDefinitions.TryAdd(typeof(EmptyStruct), new EmptyStructTypeDefinition());
        }
        
        public DynamicRuntime(Expression expression, BindingRestrictions restrictions) : base(expression, restrictions)
        {
        }

        public DynamicRuntime(Expression expression, BindingRestrictions restrictions, object value) : base(expression, restrictions, value)
        {
        }
        
        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            return binder.Name switch
            {
                "Get" => ConstructGet(binder, args),
                "New" => ConstructNew(binder, args),
                "With" => ConstructWith(binder, args),
                "IsTruthy" => ConstructIsTruthy(args),
                "VectorStruct" => ConstructVectorStruct(args),
                _ => throw new NotImplementedException($"{binder.Name} not implemented in runtime")
            };
        }

        private DynamicMetaObject ConstructVectorStruct(DynamicMetaObject[] args)
        {
            if (args.Length != 1)
                throw new Exception("Too many armors to VectorStruct");

            var arg = args[0];

            if (arg.RuntimeType != typeof(object[]))
                throw new Exception("Expected object array to vector array");
            // TODO: think about this;

            throw new NotImplementedException();
        }

        private DynamicMetaObject ConstructIsTruthy(DynamicMetaObject[] args)
        {
            if (args.Length != 1)
                throw new InvalidDataException();

            var arg = args[0];

            var def = GetDefintion(arg.Value);
            var truthy = !def.GetMembers().Any(m => ReferenceEquals(m.Item1, KW.ELSE));
            var expr = Expression.Convert(Expression.Constant(truthy), typeof(object));
            return new DynamicMetaObject(expr,
                BindingRestrictions.GetTypeRestriction(arg.Expression, arg.Value!.GetType()), truthy);
        }

        private DynamicMetaObject ConstructWith(InvokeMemberBinder binder, DynamicMetaObject[] aargs)
        {
            if (aargs.Length % 2 != 1)
                throw new InvalidDataException($"Even number of arguments to With");

            var args = new DynamicMetaObject[aargs.Length - 1];
            Array.Copy(aargs, 1, args, 0, args.Length);
            
            var keys = new List<DynamicMetaObject>();
            var vals = new List<DynamicMetaObject>();

            for (var i = 0; i < args.Length; i++)
            {
                (i % 2 == 0 ? keys : vals).Add(args[i]);
            }

            var src = aargs[0];
            var srcDef = GetDefintion(src.Value);

            var tuples = keys.Zip(vals).Select(t => new
                {
                    keyword = (Keyword)t.First.Value,
                    key = t.First,
                    valueExpr = t.Second.Expression,
                    valueType = StructConstructor.GetStructValueType(t.Second.Value!.GetType()),
                    nativeValueType = t.Second.Value!.GetType(),
                    fromStruct = false
                })
                .Concat(srcDef.GetMembers().Select(m => new
                {
                    keyword = m.Item1,
                    key = new DynamicMetaObject(Expression.Constant(m.Item1), BindingRestrictions.Empty),
                    valueExpr = srcDef.MemberGetter(src.Expression, m.Item1),
                    valueType = StructConstructor.GetStructValueType(m.Item2),
                    nativeValueType = m.Item2,
                    fromStruct = true
                }))
                .GroupBy(t => t.keyword)
                .Select(t => t.Last())
                .OrderBy(t => t.keyword)
                .ToArray();

            var tp = StructConstructor.MakeStruct(tuples.Select(t => (t.keyword, t.valueType)).ToArray());

            var ctor = tp.GetConstructor(tuples.Select(t => t.valueType).ToArray());

            var nexpr = Expression.New(ctor!, tuples.Select(t => Expression.Convert(t.valueExpr, t.valueType)).ToArray());

            var restrictions = tuples
                .Where(t => !t.fromStruct)
                .Select(t => BindingRestrictions.GetInstanceRestriction(t.key.Expression, t.keyword))
                .Concat(tuples.Where(t => !t.fromStruct).Select(t =>
                    BindingRestrictions.GetTypeRestriction(t.valueExpr, t.nativeValueType)))
                .Aggregate(BindingRestrictions.GetTypeRestriction(src.Expression, src.Value!.GetType()), (acc, n) => acc.Merge(n));

            return new DynamicMetaObject(nexpr, restrictions);

        }

        private DynamicMetaObject ConstructNew(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            if (args.Length % 2 != 0)
                throw new InvalidDataException($"Odd number of arguments to new");

            var keys = new List<DynamicMetaObject>();
            var vals = new List<DynamicMetaObject>();

            for (var i = 0; i < args.Length; i++)
            {
                (i % 2 == 0 ? keys : vals).Add(args[i]);
            }

            var tuples = keys.Zip(vals).Select(t => new
            {
                keyword = (Keyword)t.First.Value,
                key = t.First,
                value = t.Second.Value,
                valueExpr = t.Second,
                valueType = StructConstructor.GetStructValueType(t.Second.Value!.GetType())
            }).OrderBy(t => t.keyword)
                .ToArray();

            var tp = StructConstructor.MakeStruct(tuples.Select(t => (t.keyword, t.valueType)).ToArray());

            var ctor = tp.GetConstructor(tuples.Select(t => t.valueType).ToArray());

            var nexpr = Expression.New(ctor!, tuples.Select(t => t.valueExpr.Expression).ToArray());

            var restrictions = tuples
                .Select(t => BindingRestrictions.GetInstanceRestriction(t.key.Expression, t.keyword))
                .Concat(tuples.Select(t =>
                    BindingRestrictions.GetTypeRestriction(t.valueExpr.Expression, t.value!.GetType())))
                .Aggregate(BindingRestrictions.Empty, (acc, n) => acc.Merge(n));

            return new DynamicMetaObject(nexpr, restrictions);
        }

        private DynamicMetaObject ConstructGet(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            var structValue = args[0].Value;
            var definition = GetDefintion(structValue);
            var memberInfo = definition.GetMembers().FirstOrDefault(m => ReferenceEquals(m.Item1, args[1].Value));
            var restrictions = BindingRestrictions.GetTypeRestriction(args[0].Expression, args[0].RuntimeType!)
                .Merge(BindingRestrictions.GetInstanceRestriction(args[1].Expression, args[1].Value));
            
            if (memberInfo != default)
            {
                return new DynamicMetaObject(definition.MemberGetter(args[0].Expression, (Keyword) args[1].Value), restrictions);
            }

            var notFound = args.Length == 3 ? args[2].Expression : Expression.Constant(KW.MemberNotFound);
            return new DynamicMetaObject(notFound, restrictions);
        }

        public static IStructDefinition GetDefintion(object o)
        {
            return StructDefinitions[o.GetType()];
        }
        
        public static IStructDefinition GetDefintion<T>(T o)
        {
            return StructDefinitions[typeof(T)];
        }
    }
}