using System;
using System.Linq;
using Wyld.DynamicDispatch;
using Wyld.Expressions;
using Xunit;

namespace Wyld.Test
{
    public class CallSiteTests
    {
        private class SimpleBinder : ICallSiteBinder
        {
            public IExpression Bind(object[] paramValues, IExpression[] parameters, IExpression inner)
            {
                throw new NotImplementedException();
            }
        }
        private static class CallSite1
        {
            public static CallSite<IInvokableArity<string, ICallSite, int>> CallSite = CallSite<IInvokableArity<string, ICallSite, int>>.Create(new SimpleBinder());
        }
        [Fact]
        public void CanUseSimpleCallSites()
        {
            var tgt = CallSite1.CallSite.Target;
            foreach (var j in Enumerable.Range(0,3))
            {
                foreach (var i in Enumerable.Range(0, 10))
                {
                    var result = CallSite1.CallSite.Target.Invoke(CallSite1.CallSite, i);
                    Assert.Equal(i.ToString(), result.Value);

                    if (j != 0) continue;
                    Assert.Same(tgt, CallSite1.CallSite.Target);
                    tgt = CallSite1.CallSite.Target;
                }

                // After one full loop, the target should not thrash
                if (j == 0) continue; 
                
                Assert.Same(tgt, CallSite1.CallSite.Target);
            }

        }
    }
}