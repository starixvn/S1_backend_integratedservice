using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public interface ITpReportBLL
    {

        TimeCast GetReportTime(DateTime startTime, int day);

        Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime);

        IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData);

    }
}
