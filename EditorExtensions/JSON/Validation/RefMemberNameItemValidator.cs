//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Linq;
//using Microsoft.JSON.Core.Parser;
//using Microsoft.JSON.Core.Validation;
//using Microsoft.VisualStudio.Utilities;

//namespace MadsKristensen.EditorExtensions.JSON
//{
//    [Export(typeof(IJSONItemValidator))]
//    [ContentType("JSON")]
//    [Name("$ref validator")]
//    internal class RefMemberNameItemValidator : IJSONItemValidator
//    {
//        public IEnumerable<Type> ItemTypes
//        {
//            get { return new[] { typeof(JSONMember) }; }
//        }

//        public JSONItemValidationResult ValidateItem(JSONParseItem item, IJSONValidationContext context)
//        {
//            JSONMember member = item as JSONMember;

//            if (member != null && member.Name != null && member.Name.Text == "\"$ref\"")
//            {
//                var parent = member.FindType<JSONBlockItem>();

//                // Only show error when $ref is not the only property in the object
//                if (parent == null || parent.BlockItemChildren.Count() == 1)
//                    return JSONItemValidationResult.Continue;

//                JsonErrorTag error = new JsonErrorTag
//                {
//                    Flags = JSONErrorFlags.UnderlineBlue | JSONErrorFlags.ErrorListMessage,
//                    Item = member.Name,
//                    Start = member.Name.Start,
//                    AfterEnd = member.Name.AfterEnd,
//                    Length = member.Name.Length,
//                    Text = "When $ref is present, all other attributes are ignored"
//                };

//                context.AddError(error);
//            }

//            return JSONItemValidationResult.Continue;
//        }
//    }
//}
