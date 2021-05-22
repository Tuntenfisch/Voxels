using System;
using System.Collections.Generic;

namespace Tuntenfisch.Extensions
{
    public static class DictionaryExtensions
    {
        public static KeyValuePair<TKey, TValue> Pop<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            Dictionary<TKey, TValue>.Enumerator enumerator = dictionary.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Dictionary is empty.");
            }

            KeyValuePair<TKey, TValue> item = enumerator.Current;
            dictionary.Remove(item.Key);

            return item;
        }
    }
}