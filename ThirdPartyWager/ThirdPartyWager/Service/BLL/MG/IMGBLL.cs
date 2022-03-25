using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.MG
{
    public interface IMGBLL
    {
        Task ReSettle();

        Task<bool> GetReport(SummaryType summaryType, DateTime date);
    }
}