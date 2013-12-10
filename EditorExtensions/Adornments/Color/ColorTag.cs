//***************************************************************************
//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//    This code is licensed under the Visual Studio SDK license terms.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//***************************************************************************

using System.Windows.Media;
using Microsoft.VisualStudio.Text.Tagging;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Data tag indicating that the tagged text represents a color.
    /// </summary>
    /// <remarks>
    /// Note that this tag has nothing directly to do with adornments or other UI.
    /// This sample's adornments will be produced based on the data provided in these tags.
    /// This separation provides the potential for other extensions to consume color tags
    /// and provide alternative UI or other derived functionality over this data.
    /// </remarks>
    internal class ColorTag : ITag
    {
        internal readonly Color Color;

        internal ColorTag(Color color)
        {
            this.Color = color;
        }
    }
}
