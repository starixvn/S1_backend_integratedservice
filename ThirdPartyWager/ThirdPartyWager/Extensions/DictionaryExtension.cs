using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Extensions
{
    public static class DictionaryExtension
    {
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
        {
            return dic.TryGetValue(key, out TValue result) ? result : throw new ArgumentException($"{key} is empty");
        }
    }
}