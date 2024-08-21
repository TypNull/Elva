using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Elva.MVVM.Model.Export
{
    internal class ExplorerComparer : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string? x, string? y);
        public int Compare(string? x, string? y) => StrCmpLogicalW(x, y);
    }
}
