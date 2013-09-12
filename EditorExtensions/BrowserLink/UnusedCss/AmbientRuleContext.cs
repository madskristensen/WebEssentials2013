using System;
using System.Collections.Generic;
using System.Threading;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    internal class AmbientRuleContext : IDisposable
    {
        private static readonly AmbientRuleContext Instance = new AmbientRuleContext();
        private int _referenceCount;

        public void Update()
        {
            Rules = RuleRegistry.GetAllRules();
        }

        public IReadOnlyCollection<IStylingRule> Rules { get; private set; }

        public static AmbientRuleContext GetOrCreate()
        {
            if (Interlocked.Increment(ref Instance._referenceCount) == 1)
            {
                Instance.Update();
            }

            return Instance;
        }

        public static IReadOnlyCollection<IStylingRule> GetAllRules()
        {
            using (GetOrCreate())
            {
                return Instance.Rules;
            }
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref _referenceCount);
        }
    }
}