using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elva.MVVM.Model.Database.Saveable
{
    internal class SaveableComparer : IEqualityComparer<SaveableOnlineData>
    {

        public bool Equals(SaveableOnlineData? x, SaveableOnlineData? y)
        {
            return x?.Url.Equals(y?.Url) == true;
        }



        public int GetHashCode([DisallowNull] SaveableOnlineData obj)
        {
            return obj.Url.GetHashCode();
        }
    }
}
