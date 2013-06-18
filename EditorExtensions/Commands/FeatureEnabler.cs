using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.JavaScript.Web.Extensions.Shared;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IFeatureEnabler))]
    internal class FeatureEnabler : IFeatureEnabler
    {
        private static string[] _enabledFeatures = 
        {
            FeatureManager.Features.DocCommentExtension,
            FeatureManager.Features.DocCommentScaffolding
        };

        public IEnumerable<string> EnabledFeatures
        {
            get
            {
                return _enabledFeatures;
            }
        }
    }
}
