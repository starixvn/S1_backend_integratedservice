namespace ThirdPartyWager.Service.BLL.MG
{
    public interface IMGBLL
    {
        Task ReSettle();

        Task<bool> GetReport(DateTime date);
    }
}