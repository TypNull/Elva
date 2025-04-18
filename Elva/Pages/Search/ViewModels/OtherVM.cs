using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Search.Models;
using Elva.Pages.Shared.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace Elva.Pages.Search.ViewModels
{
    internal partial class OtherVM : ViewModelObject
    {
        public bool IsVisible { get => _isVisible; private set => SetProperty(ref _isVisible, value); }
        private bool _isVisible;

        [ObservableProperty]
        private ObservableCollection<KeyVal<string, string[], string, bool>> _radioTags = new();

        private SearchManager _searchManager;

        public OtherVM(INavigationService navigation) : base(navigation)
        {
            _searchManager = GetService<SearchManager>();
            _searchManager.OnSearchChanged += OnSearchChanged;
            _radioTags = new(_searchManager.ActualSearch.RadioTags.Values.Select(x => new KeyVal<string, string[], string, bool>(x.Title, x.Tags.Where(x => x.Key != "notset").Select(x => x.Title).ToArray(), x.EnabledTag?.Title ?? x.DefaultKey, x.Tags.Any(x => x.Key == "notset"))));
            IsVisible = _radioTags.Any();
        }

        private void OnSearchChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "RadioTags")
            {
                RadioTags = new(_searchManager.ActualSearch.RadioTags.Values.Select(x => new KeyVal<string, string[], string, bool>(x.Title, x.Tags.Where(x => x.Key != "notset").Select(x => x.Title).ToArray(), x.EnabledTag?.Title ?? x.DefaultKey, x.Tags.Any(x => x.Key == "notset"))));
            }
        }

        [RelayCommand]
        private void ChangeTag(object tag)
        {
            object[] values = (object[])tag;
            KeyVal<string, string[], string, bool> raioTag = (KeyVal<string, string[], string, bool>)values[0];
            string changed = (string)values[1];
            KeyVal<string, string[], string, bool>? keyVal = RadioTags.FirstOrDefault(x => x.Key == raioTag.Key);
            if (keyVal == null)
                return;
            string oldValue = keyVal.SecondValue;
            if (keyVal.SecondValue.Equals(changed))
                keyVal.SecondValue = "notset";
            else
                keyVal.SecondValue = changed;
            if (!keyVal.ThirdValue && keyVal.SecondValue == "notset")
                keyVal.SecondValue = changed;
            if (keyVal.SecondValue != oldValue)
                _searchManager.ActualSearch.SetRadioTag(keyVal.Key.ToLower(), keyVal.SecondValue.ToLower());
        }
    }
}
