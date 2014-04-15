using System;
using Microsoft.Practices.TransientFaultHandling;

namespace MadsKristensen.EditorExtensions
{
    public class PolicyFactory
    {
        public static RetryPolicy GetPolicy(ITransientErrorDetectionStrategy strategy, int retryCount)
        {
            RetryPolicy policy = new RetryPolicy(strategy, retryCount, TimeSpan.FromMilliseconds(50));

            return policy;
        }
    }
}
