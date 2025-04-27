using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;

namespace Elva.Common.Navigation
{
    public interface INavigationService : INotifyPropertyChanged
    {
        ViewModelObject CurrentView { get; }
        void NavigateTo<T>() where T : ViewModelObject;
        void NavigateTo(Type type);
    }

    public partial class NavigationService(Func<Type, ViewModelObject> viewModelFactory) : ObservableObject, INavigationService
    {
        [ObservableProperty]
        private ViewModelObject _currentView = null!;
        private readonly Func<Type, ViewModelObject> _viewModelFactory = viewModelFactory;

        public void NavigateTo<TViewModel>() where TViewModel : ViewModelObject
        {
            ViewModelObject vm = _viewModelFactory.Invoke(typeof(TViewModel));
            CurrentView = vm;
            OnPropertyChanged(nameof(INavigationService.CurrentView));
        }

        public void NavigateTo(Type type)
        {
            if (!type.IsSubclassOf(typeof(ViewModelObject)))
                throw new NotSupportedException(type.FullName);
            ViewModelObject vm = _viewModelFactory.Invoke(type);
            CurrentView = vm;

            OnPropertyChanged(nameof(INavigationService.CurrentView));
        }
    }
}
