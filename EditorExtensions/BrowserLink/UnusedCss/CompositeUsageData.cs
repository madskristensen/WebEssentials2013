using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CompositeUsageData : IUsageDataSource
    {
        private readonly HashSet<CssRule> _allRules = new HashSet<CssRule>();
        private readonly Project _project;
        private readonly HashSet<RuleUsage> _ruleUsages = new HashSet<RuleUsage>();
        private readonly List<IUsageDataSource> _sources = new List<IUsageDataSource>();
        private readonly object _sync = new object();
        private readonly HashSet<CssRule> _unusedRules = new HashSet<CssRule>();

        public CompositeUsageData(Project project)
        {
            _project = project;
        }

        public void AddUsageSource(IUsageDataSource source)
        {
            lock (_sync)
            {
                _sources.Add(source);
                _allRules.UnionWith(source.GetAllRules());
                _ruleUsages.UnionWith(source.GetRuleUsages());
                _unusedRules.IntersectWith(source.GetUnusedRules());
            }
        }

        public IEnumerable<CssRule> GetAllRules()
        {
            lock (_sync)
            {
                return _allRules;
            }
        }

        public IEnumerable<RuleUsage> GetRuleUsages()
        {
            lock (_sync)
            {
                return _ruleUsages;
            }
        }

        public IEnumerable<CssRule> GetUnusedRules()
        {
            lock (_sync)
            {
                return _unusedRules;
            }
        }

        public IEnumerable<Task> GetWarnings()
        {
            lock(_sync)
            {
                return _unusedRules.Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _project, "Unused CSS rule \"{0}\""));
            }
        }
        
        public IEnumerable<Task> GetWarnings(Uri uri)
        {
            lock(_sync)
            {
                return _unusedRules.Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _project, "Unused CSS rule \"{0}\" on page " + uri));
            }
        }
    }
}