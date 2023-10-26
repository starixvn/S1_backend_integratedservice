namespace ThirdPartyBetHistory.Service.Model.enums
{
    public enum WorkerType
    {
        MainWager = 1,                  //使用TicketKey固定持續拉單
        ByTransIdUnSettleWager = 3,     //檢查未結算注單
        RngWager = 5,
        VgamingUnSettle = 6,            //檢查Vgaming未結算，重打Saba Resubmit Process
    }
}
