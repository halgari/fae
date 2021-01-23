using System;
using System.Reflection;

namespace Wyld.Expressions
{
    public class Expression
    {
        public static Constant Constant(object? val, Type? tp = null) => new(val!, tp ?? val!.GetType());

        public static NullConstant NullConstant(Type type) => new(type);

        public static IExpression Convert<T>(IExpression expr) => new Convert<T>(expr);

        public static If If(IExpression test, IExpression then, IExpression els) => new(test, then, els);

        public static Add Add(IExpression a, IExpression b) => new(a, b);
        public static Equals Equals(IExpression a, IExpression b) => new(a, b);

        public static Property Property(IExpression source, PropertyInfo prop) => new(source, prop);
        public static Field Field(IExpression source, FieldInfo field) => new(source, field);

        public static AssignToField Assign(Field field, IExpression value) => new(field, value);

        public static Invoke Invoke(IExpression fn, IExpression[] args)
        {
            if (fn.Type.IsAssignableTo(typeof(IInvokable)))
            {
                return new InvokableInvoke(fn, args);
            }

            throw new NotImplementedException();
        }

        public static Parameter Parameter(string name, Type type, int idx) => new(name, type, idx);

        public static Do Do(IExpression[] body) => new(body);
        
        public static Lambda Lambda(string name, Arity[] arities) => new(name, arities);

        public static This This(string name, Type type) => new This(name, type);

        public static FreeVariable FreeVariable(ILocal source) => new(source);
    }
}