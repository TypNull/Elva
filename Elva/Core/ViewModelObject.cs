using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Elva.Core
{
    public partial class ViewModelObject : ObservableObject
    {
        private INavigationService _navigation;

        public INavigationService Navigation => _navigation;

        public ViewModelObject(INavigationService navigation) => _navigation = navigation;

        protected static TObject GetService<TObject>() where TObject : notnull => App.Current.ServiceProvider.GetRequiredService<TObject>();

    }
}
