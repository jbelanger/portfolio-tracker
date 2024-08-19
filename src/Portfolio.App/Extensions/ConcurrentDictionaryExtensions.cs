using System.Collections.Concurrent;

namespace Portfolio.App;

public static class ConcurrentDictionaryExtensions
{
    public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
        this ConcurrentDictionary<TKey, Lazy<Task<TValue>>> dictionary,
        TKey key,
        Func<TKey, Task<TValue>> valueFactory) where TKey : notnull
    {
        var lazyValue = dictionary.GetOrAdd(key, k => new Lazy<Task<TValue>>(() => valueFactory(k)));

        try
        {
            return await lazyValue.Value;
        }
        catch
        {
            // If the valueFactory throws an exception, remove the key to avoid leaving an invalid entry in the dictionary.
            dictionary.TryRemove(key, out _);
            throw;
        }
    }

    public static async Task<bool> TryAddAsync<TKey, TValue>(
        this ConcurrentDictionary<TKey, Lazy<Task<TValue>>> dictionary,
        TKey key,
        Func<TKey, Task<TValue>> valueFactory) where TKey : notnull
    {
        var lazyValue = new Lazy<Task<TValue>>(() => valueFactory(key));

        if (dictionary.TryAdd(key, lazyValue))
        {
            try
            {
                await lazyValue.Value;
                return true;
            }
            catch
            {
                // If the valueFactory throws an exception, remove the key to avoid leaving an invalid entry in the dictionary.
                dictionary.TryRemove(key, out _);
                throw;
            }
        }

        return false; // The key already exists, so we don't add a new value.
    }

    public static async Task<TValue> AddOrUpdateAsync<TKey, TValue>(
        this ConcurrentDictionary<TKey, Lazy<Task<TValue>>> dictionary,
        TKey key,
        Func<TKey, Task<TValue>> addValueFactory,
        Func<TKey, TValue, Task<TValue>> updateValueFactory) where TKey : notnull
    {
        var lazyValue = new Lazy<Task<TValue>>(() => addValueFactory(key));

        var result = dictionary.AddOrUpdate(
            key,
            lazyValue,
            (k, existingLazyValue) => new Lazy<Task<TValue>>(async () =>
            {
                var existingValue = await existingLazyValue.Value;
                return await updateValueFactory(k, existingValue);
            }));

        try
        {
            return await result.Value;
        }
        catch
        {
            dictionary.TryRemove(key, out _);
            throw;
        }
    }
}
