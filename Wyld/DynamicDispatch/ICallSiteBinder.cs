using Wyld.Expressions;

namespace Wyld.DynamicDispatch
{
    public interface ICallSiteBinder
    {
        public IExpression Bind(object[] paramValues, IExpression[] parameters, IExpression inner);
    }
}