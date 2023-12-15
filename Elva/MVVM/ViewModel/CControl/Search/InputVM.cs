using CommunityToolkit.Mvvm.ComponentModel;
using Elva.Core;
using Elva.MVVM.Model;
using Elva.MVVM.Model.Manager;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Elva.MVVM.ViewModel.CControl.Search
{
    internal partial class InputVM : ViewModelObject
    {
        public bool IsVisible { get => _isVisible; private set => SetProperty(ref _isVisible, value); }
        private bool _isVisible;

        [ObservableProperty]
        private ObservableCollection<KeyVal<string, string>> _inputFields = new();

        private SearchManager _searchManager;

        public InputVM(INavigationService navigation) : base(navigation)
        {
            _searchManager = GetService<SearchManager>();
            _searchManager.OnSearchChanged += SearchManager_OnSearchChanged;
            InputFields = new(_searchManager.ActualSearch.TextTags.Select(x => new KeyVal<string, string>(x)));
            foreach (KeyVal<string, string> field in InputFields)
                field.PropertyChanged += Field_PropertyChanged;
            IsVisible = InputFields.Any();
        }

        private void Field_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is KeyVal<string, string> keyVal)
                _searchManager.ActualSearch.SetTextTag(keyVal.Key, keyVal.Value);

        }

        private void SearchManager_OnSearchChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TextTags")
            {
                foreach (KeyVal<string, string> field in InputFields)
                    field.PropertyChanged -= Field_PropertyChanged;
                InputFields = new(_searchManager.ActualSearch.TextTags.Select(x => new KeyVal<string, string>(x)));
                foreach (KeyVal<string, string> field in InputFields)
                    field.PropertyChanged += Field_PropertyChanged;
            }
            else if (e.PropertyName?.StartsWith("TextTags_") == true)
            {
                KeyVal<string, string>? field = InputFields.Where(x => x.Key == e.PropertyName.Substring(9)).FirstOrDefault();
                if (field != null)
                    field.Value = _searchManager.ActualSearch.TextTags.GetValueOrDefault(field.Key) ?? field.Value;
            }
        }
    }
}
