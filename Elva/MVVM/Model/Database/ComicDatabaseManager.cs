using Elva.MVVM.Model.Database.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Elva.MVVM.Model.Database
{
    internal class ComicDatabaseManager : DatabaseManager<ComicContext>
    {
        public override void LoadData()
        {
            Context.Comics.Include(x => x.Chapter.OrderBy(chapter => chapter.Order)).Load();
        }
    }
}
