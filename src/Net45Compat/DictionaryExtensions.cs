﻿#if NET45
#pragma warning disable IDE0130
using System.Collections.Generic;

namespace TheXDS.CoreBlocks;

internal static class DictionaryExtensions
{
    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.ContainsKey(key))
        {
            return false;
        }
        dictionary.Add(key, value);
        return true;
    }
}
#endif