//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using Microsoft.CSS.Core;
//using Microsoft.VisualStudio.Utilities;
//using System.IO;
//using System;

//namespace MadsKristensen.EditorExtensions
//{
//    [Export(typeof(ICssErrorFilter))]
//    [Name("ScssErrorFilter")]
//    [Order(After = "Default")]
//    internal class ScssErrorFilter : ICssErrorFilter
//    {
//        public void FilterErrorList(IList<ICssError> errors, ICssCheckerContext context)
//        {
//            var document = EditorExtensionsPackage.DTE.ActiveDocument;

//            if (document == null || !Path.GetExtension(document.FullName).Equals(ScssContentTypeDefinition.ScssFileExtension, StringComparison.OrdinalIgnoreCase))
//                return;

//            for (int i = errors.Count - 1; i > -1; i--)
//            {
//                ICssError error = errors[i];
                
//                if ((error.Flags & CssErrorFlags.TaskListError) == CssErrorFlags.TaskListError)
//                {
//                    errors.RemoveAt(i);
//                }
//            }
//        }
//    }
//}