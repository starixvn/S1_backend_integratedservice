namespace ThirdPartyWager.Service.BLL
{
    public interface IMGBLL
    {
        Task ReSettle();

        Task<bool> GetReport(DateTime date);
    }
}