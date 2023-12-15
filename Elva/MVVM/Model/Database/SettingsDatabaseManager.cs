using Elva.MVVM.Model.Database.Context;
using Elva.MVVM.Model.Database.Saveable;
using Microsoft.EntityFrameworkCore;

namespace Elva.MVVM.Model.Database
{
    internal class SettingsDatabaseManager : DatabaseManager<SettingsContext>
    {
        private bool _websiteReferenceChanged = false;
        public void Add(WebsiteHomeReference data)
        {
            _websiteReferenceChanged = true;
            base.Add(data);
        }

        public void Add(ComicReference data)
        {
            if (data.ReferenceType == "Info")
                lock (_lockObject)
                {
                    Context.DeleteComicReferences("Info");
                }
            base.Add(data);
        }

        public override void LoadData()
        {
            Context.WebsiteReferences.Load();
            Context.ComicReferences.Load();
        }

        protected override void SaveChanges(params object[] addable)
        {
            lock (_lockObject)
            {
                if (_websiteReferenceChanged)
                    Context.ClearWebsiteReference();
                Context.AddRange(addable);
                Context.SaveChanges();
            }
        }
    }
}
