using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.Model;

public class BatchTpUnsettleModel
{
    public Tp Tp { get; set; }
    public GameType GameType { get; set; }
    public IEnumerable<TpUnsettleInfo> Infos { get; set; }
    public DateTime? PartitionTime { get; set; } = null;
}

public class TpUnsettleInfo
{
    public string TransId { get; set; }
}