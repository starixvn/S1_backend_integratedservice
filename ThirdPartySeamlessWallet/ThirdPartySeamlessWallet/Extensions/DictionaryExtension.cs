namespace ThirdPartySeamlessWallet.Extensions
{
    public static class DictionaryExtension
    {
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
        {
            return dic.TryGetValue(key, out TValue result) ? result : throw new ArgumentException($"{key} is empty");
        }

        public static string GetKey(this IDictionary<string, string> dic, string value)
        {
            return dic.FirstOrDefault(x => x.Value == value).Key;
        }

        public static decimal GetValueDecimal(this IDictionary<string, string> dic, string key)
        {
            return dic.TryGetValue(key, out string value) ? decimal.Parse(value) : 0;
        }
    }
}
