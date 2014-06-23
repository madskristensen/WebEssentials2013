using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.Editor;
using VS = Microsoft.VisualStudio;

namespace MadsKristensen.EditorExtensions
{
    internal static class OptionHelpers
    {
        private static int _fontSize;
        private static ColorModel _backgroundColor;
        private static object _syncRoot = new object();

        // TODO: Compensate for the current line highlighting
        public static ColorModel BackgroundColor
        {
            get
            {
                if (_backgroundColor == null)
                {
                    lock (_syncRoot)
                    {
                        if (_backgroundColor == null)
                        {
                            GetSize();
                        }
                    }
                }

                return _backgroundColor;
            }
        }

        public static int FontSize
        {
            get
            {
                if (_fontSize == 0)
                {
                    lock (_syncRoot)
                    {
                        if (_fontSize == 0)
                        {
                            GetSize();
                        }
                    }
                }

                return _fontSize;
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        private static void GetSize()
        {
            try
            {
                IVsFontAndColorStorage storage = (IVsFontAndColorStorage)WebEssentialsPackage.GetGlobalService(typeof(IVsFontAndColorStorage));
                var guid = new Guid("A27B4E24-A735-4d1d-B8E7-9716E1E3D8E0");
                if (storage != null && storage.OpenCategory(ref guid, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) == VS.VSConstants.S_OK)
                {
                    LOGFONTW[] Fnt = new LOGFONTW[] { new LOGFONTW() };
                    FontInfo[] Info = new FontInfo[] { new FontInfo() };
                    storage.GetFont(Fnt, Info);
                    _fontSize = (int)Info[0].wPointSize;
                }

                if (storage != null && storage.OpenCategory(ref guid, (uint)(__FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) == VS.VSConstants.S_OK)
                {
                    var info = new ColorableItemInfo[1];
                    storage.GetItem("Plain Text", info);
                    _backgroundColor = ConvertFromWin32Color((int)info[0].crBackground);
                }

            }
            catch { }
        }

        public static ColorModel ConvertFromWin32Color(int color)
        {
            int r = color & 0x000000FF;
            int g = (color & 0x0000FF00) >> 8;
            int b = (color & 0x00FF0000) >> 16;
            return new ColorModel() { Red = r, Green = g, Blue = b };
        }
    }
}
