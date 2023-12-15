using CommunityToolkit.Mvvm.ComponentModel;
using Elva.Core;
using Elva.MVVM.Model.Manager;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;

namespace Elva.MVVM.ViewModel.CControl.Home
{
    internal partial class HomeVM : ViewModelObject
    {
        [ObservableProperty]
        public ObservableCollection<HomeWebsiteVM> _websites = null!;
        private WebsiteManager _websiteManagaer;

        public HomeVM(INavigationService navigation) : base(navigation)
        {
            _websiteManagaer = App.Current.ServiceProvider.GetRequiredService<WebsiteManager>();
            SetWebsiteCollection();
            _websiteManagaer.WebsiteAdded += (s, e) => SetWebsiteCollection();
        }

        private void SetWebsiteCollection() => Websites = new ObservableCollection<HomeWebsiteVM>(_websiteManagaer.Websites.Select(w => new HomeWebsiteVM(w)));
    }
}
