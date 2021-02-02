using System;
using System.Linq;
using Wyld.Expressions;

namespace Wyld.DynamicDispatch
{
    public class CallSite<T> : ICallSite
    where T : IInvokable
    {
        private ICallSiteBinder _binder;
        public T Target;

        private CallSite(ICallSiteBinder binder)
        {
            _binder = binder;
        }

        public static CallSite<T> Create(ICallSiteBinder binder)
        {
            return new(binder);
        }

        private void InitTarget()
        {
            var invokeMethod = typeof(T).GetMethod("Invoke");
            var parameters = invokeMethod.GetParameters()
                .Select((p, idx) => Expression.Parameter("arg" + idx, p.ParameterType, idx))
                .ToArray();

            var updateMethod = typeof(CallSite<T>).GetMethod("UpdateCallsite")!
                .MakeGenericMethod(invokeMethod.ReturnType);
            var body = Expression.StaticMethod(updateMethod,
                new IExpression[] {parameters[0], Expression.MakeArray(parameters.Skip(1).Select(Expression.Convert<object>).ToArray())});

            var arity = new Arity(parameters, body);
            var lmbda = Expression.Lambda("callsite", new[] {arity});
            //Target = (T)lmbda.Compile();
        }

        public static TR UpdateCallsite<TR>(CallSite<T> site, object[] args)
        {
            throw new NotImplementedException();
        }
        
    }
}