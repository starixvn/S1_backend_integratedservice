using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class TicketAGBLL
    {

        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly ApiAGBLL apiAGBLL;
        private readonly ILogger<TicketAGBLL> logger;

        public TicketAGBLL(ILogger<TicketAGBLL> logger, ApiAGBLL apiAGBLL, ThirdPartyBLL thirdPartyBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.apiAGBLL = apiAGBLL;
            this.logger = logger;
        }

        public async Task<dynamic> CheckTicketAsync()
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.AG);
                string transactionID = "005211082122001585411";
                var data = await apiAGBLL.CheckTicketStatus(setting["WalletUrl"], transactionID);
                if (data.result == null)
                {
                    logger.LogError(null);
                    logger.LogInformation($"THIRD_PARTY_ERROR");
                    return false;
                }

                var dataTicket = data.result.row;
                var ticket = new
                {
                    billNo = dataTicket.billNo,
                    status = dataTicket.status
                };
                return ticket;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return true;
            }
        }
    }
}
