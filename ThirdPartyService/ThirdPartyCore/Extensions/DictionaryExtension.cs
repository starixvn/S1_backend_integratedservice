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
            return dictionary.GetValueOrDefault(key, default)?.ToString() ?? string.Empty;
        }

        public static string GetFirstStringValue<TKey, TValue>(this IEnumerable<IReadOnlyDictionary<TKey, TValue>> list, TKey key)
        {
            foreach (var dic in list)
            {
                var str = dic.GetStringValue(key);
                if (!string.IsNullOrEmpty(str))
                {
                    return str;
                }
            }
            return string.Empty;
        }
    }
}