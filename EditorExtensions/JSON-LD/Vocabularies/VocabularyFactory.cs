using System;
using System.Collections.Generic;
using System.Linq;
using MadsKristensen.EditorExtensions.JSON;
using Microsoft.JSON.Core.Parser;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.JSONLD
{
    class VocabularyFactory
    {
        private static IEnumerable<Lazy<IVocabulary>> _vocabularies;

        public static IEnumerable<IVocabulary> GetAllVocabularies()
        {
            if (_vocabularies == null)
            {
                _vocabularies = ComponentLocator<IVocabulary>.ImportMany();
            }

            return _vocabularies.Select(v => v.Value);
        }

        public static IEnumerable<IVocabulary> GetVocabularies(JSONParseItem item)
        {
            var vocabs = GetAllVocabularies();
            return FindApplicableVocabularies(item, vocabs);
        }

        private static IEnumerable<IVocabulary> FindApplicableVocabularies(JSONParseItem member, IEnumerable<IVocabulary> vocabularies)
        {
            var parent = member.FindType<JSONBlockItem>();

            while (parent != null)
            {
                var visitor = new JSONItemCollector<JSONMember>();
                parent.Accept(visitor);

                var context = visitor.Items.FirstOrDefault(c => c.Name != null && c.Name.Text == "\"@context\"");

                foreach (IVocabulary vocab in vocabularies)
                {
                    if (vocab.AppliesToContext(context))
                        yield return vocab;
                }

                parent = parent.Parent.FindType<JSONBlockItem>();
            }
        }
    }
}