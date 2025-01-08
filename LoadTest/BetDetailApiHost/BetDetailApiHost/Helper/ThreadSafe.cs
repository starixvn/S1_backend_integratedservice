namespace BetDetailApiHost.Helper;

using System;

public class ThreadSafe<T>(T initialValue = default)
{
    private T _value = initialValue;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public async Task<T> GetValueAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _value;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetValueAsync(T value)
    {
        await _semaphore.WaitAsync();
        try
        {
            _value = value;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateValueAsync(Func<T, Task<T>> updateFunc)
    {
        await _semaphore.WaitAsync();
        try
        {
            _value = await updateFunc(_value);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateValueAsync(Func<T, T> updateFunc)
    {
        await _semaphore.WaitAsync();
        try
        {
            _value = updateFunc(_value);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> TryGetValueAsync(Predicate<T> predicate, Func<T, Task> onSuccess)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (predicate(_value))
            {
                await onSuccess(_value);
                return true;
            }
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
