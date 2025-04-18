﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using System;
using System.Windows.Shell;

namespace Elva.Pages.Windows.ViewModel
{
    internal partial class MainWindowVM : ViewModelObject
    {
        [ObservableProperty]
        private float _progressValue = 0f;

        [ObservableProperty]
        private TaskbarItemProgressState _taskbarState = TaskbarItemProgressState.Normal;

        public string CurrentName => Navigation.CurrentView.GetType().Name[..^2];

        public MainWindowVM(INavigationService navigation) : base(navigation)
        {
            Navigation.PropertyChanged += NavigationService_PropertyChanged;
        }

        [RelayCommand]
        private void ChangeView(string name)
        {
            if (Type.GetType($"Elva.Pages.{name}.ViewModels.{name}VM") is Type viewModel)
                Navigation.NavigateTo(viewModel);
            OnPropertyChanged(nameof(CurrentName));
        }

        private void NavigationService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentView")
                OnPropertyChanged(nameof(CurrentName));
        }
    }
}
