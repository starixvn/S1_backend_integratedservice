using System;
using System.Collections.Generic;

namespace ThirdPartyCore.Extensions
{
    public static class DictionaryExtension
    {
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
        {
            return dic.TryGetValue(key, out TValue result) ? result : throw new ArgumentException($"{key} is empty");
        }

        public static string GetStringValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.GetValueOrDefault(key)?.ToString() ?? string.Empty;
        }
    }
}