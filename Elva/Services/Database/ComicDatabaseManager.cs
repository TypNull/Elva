using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Elva.Services.Database
{
    internal class ComicDatabaseManager : DatabaseManager<ComicContext>
    {
        public override async Task LoadDataAsync()
        {
            await Context.Comics.Include(x => x.Chapter.OrderBy(chapter => chapter.Order)).LoadAsync();
        }
    }
}
