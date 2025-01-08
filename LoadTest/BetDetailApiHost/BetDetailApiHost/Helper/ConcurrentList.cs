namespace BetDetailApiHost.Helper;

internal class ConcurrentList<T>
{
    private readonly List<T> list = new List<T>();
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    private async Task<TValue> ExecuteAsync<TValue>(Func<List<T>, TValue> action)
    {
        await semaphore.WaitAsync();
        try
        {
            return action(list);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task ExecuteAsync(Action<List<T>> action)
    {
        await semaphore.WaitAsync();
        try
        {
            action(list);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public int Count => list.Count;

    public async Task Add(T item) => await ExecuteAsync(list => list.Add(item));

    public async Task<bool> Remove(T item) => await ExecuteAsync(list => list.Remove(item));

    public async Task<T> Get(int index) => await ExecuteAsync(list => list[index]);

    public async Task<List<T>> GetAll() => await ExecuteAsync(list => new List<T>(list));

    public async Task Clear() => await ExecuteAsync(list => list.Clear());

    public async Task<T> FirstOrDefault(Func<T, bool> predicate) => await ExecuteAsync(list => list.FirstOrDefault(predicate));

    public async Task<T> Find(Func<T, bool> predicate) => await ExecuteAsync(list => list.Find(new Predicate<T>(predicate)));
}