using System.Collections.Concurrent;

namespace Portfolio.App;

public static class ConcurrentDictionaryExtensions
{
    public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, Task<TValue>> valueFactory)
    {
        if (dictionary.TryGetValue(key, out var existingValue))
        {
            return existingValue;
        }

        var newValue = await valueFactory(key);

        // TryAdd will return false if another thread added the key before us
        return dictionary.GetOrAdd(key, newValue);
    }

    public static async Task<bool> TryAddAsync<TKey, TValue>(
    this ConcurrentDictionary<TKey, TValue> dictionary,
    TKey key,
    Func<TKey, Task<TValue>> valueFactory)
    {
        if (dictionary.ContainsKey(key))
        {
            return false; // The key already exists, so we don't add a new value.
        }

        var newValue = await valueFactory(key);

        return dictionary.TryAdd(key, newValue);
    }
}