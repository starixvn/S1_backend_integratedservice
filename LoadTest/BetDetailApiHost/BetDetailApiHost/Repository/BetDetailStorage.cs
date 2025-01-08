using System.Collections.Concurrent;
using BetDetailApiHost.Helper;
using BetDetailApiHost.Models.GetBetDetail;

namespace BetDetailApiHost.Repository;

public interface IBetDetailStorage
{
    public Task<List<BetDetail>> GetBetDetailsAsync();

    public List<BetDetail> GetBetDetailsListAsync();

    public Task TryAddBatchBetDetailAsync(List<BetDetail> value);

    public int Count();

    public Task RemoveUnusedBetDetail();
}

public class BetDetailStorage : IBetDetailStorage
{
    private readonly ConcurrentDictionary<string, BetDetail> Storage = new();
    private readonly ThreadSafe<int> LastVersionKey = new(0);
    private readonly ThreadSafe<bool> IsUpdated = new(false);

    public int Count()
    {
        return Storage.Values.Count;
    }

    public async Task TryAddBatchBetDetailAsync(List<BetDetail> value)
    {
        foreach (var item in value)
        {
            await TryAddBetDetailAsync(item);
        }
    }

    private async Task TryAddBetDetailAsync(BetDetail value)
    {
        await IsUpdated.UpdateValueAsync(async _ =>
        {
            value.version_key = await LastVersionKey.GetValueAsync();
            Storage.AddOrUpdate($"{value.operator_id}_{value.ref_id}", value, (_, _) => value);

            return true;
        });
    }

    public async Task RemoveUnusedBetDetail()
    {
        var lastVersionKey = await LastVersionKey.GetValueAsync();

        foreach (var item in Storage)
        {
            if (item.Value.version_key < lastVersionKey)
            {
                Storage.TryRemove($"{item.Value.operator_id}_{item.Value.ref_id}", out var result);
            }
        }
    }

    public async Task<List<BetDetail>> GetBetDetailsAsync()
    {
        var lastVersionKey = await LastVersionKey.GetValueAsync();
        var result = Storage.Values.Where(betDetail => betDetail.version_key >= lastVersionKey).ToList();

        await IsUpdated.UpdateValueAsync(async isUpdated =>
        {
            if (isUpdated)
            {
                await LastVersionKey.UpdateValueAsync(lastVersionKey => lastVersionKey + 1);
            }

            return false;
        });

        return result;
    }

    public List<BetDetail> GetBetDetailsListAsync()
    {
        var result = Storage.Values.ToList();


        return result;
    }
}