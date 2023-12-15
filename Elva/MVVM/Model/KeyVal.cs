using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Elva.MVVM.Model
{
    public class KeyVal<TKey, TVal> : ObservableObject
    {
        public TKey Key { get => _key; set => SetProperty(ref _key, value); }
        private TKey _key;
        public TVal Value { get => _value; set => SetProperty(ref _value, value); }
        private TVal _value;

        public KeyVal(KeyValuePair<TKey, TVal> keyValuePair)
        {
            _key = keyValuePair.Key;
            _value = keyValuePair.Value;
        }

        public KeyVal(TKey key, TVal val)
        {
            _key = key;
            _value = val;
        }
    }

    public class KeyVal<TKey, TVal1, TVal2> : KeyVal<TKey, TVal1>
    {
        public TVal2 SecondValue { get => _secondValue; set => SetProperty(ref _secondValue, value); }
        private TVal2 _secondValue;


        public KeyVal(TKey key, TVal1 val1, TVal2 val2) : base(key, val1)
        {
            _secondValue = val2;
        }
    }

    public class KeyVal<TKey, TVal1, TVal2, TVal3> : KeyVal<TKey, TVal1, TVal2>
    {
        public TVal3 ThirdValue { get => _thirdValue; set => SetProperty(ref _thirdValue, value); }
        private TVal3 _thirdValue;


        public KeyVal(TKey key, TVal1 val1, TVal2 val2, TVal3 val3) : base(key, val1, val2)
        {
            _thirdValue = val3;
        }
    }
}
