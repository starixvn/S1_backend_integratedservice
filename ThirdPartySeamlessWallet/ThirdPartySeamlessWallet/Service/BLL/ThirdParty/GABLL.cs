using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Sockets;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class GABLL : IGABLL
    {
        private readonly MySqlClient _mainDbClient;
        private readonly PartitionService _partitionService;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        public GABLL(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        }

        public int Bet(TpCustInfo tpCustInfo, GATransferReq reqData)
        {
            var tempResult = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            decimal afterAmount = 0;
            var partitionTime = reqData.ticket.created_date_time;

            var check = _partitionService.Query<BettingData_GA>(partitionTime, "Service_Bettrans_GA_Check", new { id = reqData.id });

            if (check.Any())
            {
                return (int)GATransactionStatus.Success;
            }

            var bettingData = new BettingData_GA()
            {
                id = reqData.id,
                member_ref_id = reqData.member_ref_id,
                type = reqData.type,
                account_id = reqData.ticket.account_id,
                arena_code = reqData.ticket.arena_code,
                bet_amount = Convert.ToDecimal(reqData.ticket.bet_amount),
                ticket_amount = Convert.ToDecimal(reqData.ticket.ticket_amount),
                ante_amount = Convert.ToDecimal(reqData.ticket.ante_amount),
                created_date_time = reqData.ticket.created_date_time,
                fight_number = Convert.ToInt32(reqData.ticket.fight_number),
                match_day_code = reqData.ticket.match_day_code,
                modified_date_time = reqData.ticket.modified_date_time,
                odds = Convert.ToDecimal(reqData.ticket.odds),
                result = reqData.ticket.result,
                selection = reqData.ticket.selection,
                settled_date_time = reqData.ticket.settled_date_time == null ? DateTime.MinValue : reqData.ticket.settled_date_time,
                sid = reqData.ticket.sid,
                status = reqData.ticket.status,
                win_loss_amount = Convert.ToDecimal(reqData.ticket.win_loss_amount),
                amount = Convert.ToDecimal(reqData.amount),
            };

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.GA, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.bet_amount *= tpCurrency.ExchangeRate;
                bettingData.ticket_amount *= tpCurrency.ExchangeRate;
                bettingData.ante_amount *= tpCurrency.ExchangeRate;
                bettingData.win_loss_amount *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_Bettrans_GA_Ins", bettingData, trans);

            tempResult = _bettingService.PlaceBetV2(tpCustInfo, Math.Abs(bettingData.amount) * -1, (int)Tp.GA, bettingData.sid, (int)GameType.GA_CockFighting, bettingData.sid, null, TpCategory.Cockfighting, out afterAmount, ref bonusId, conn, trans);

            return tempResult > 0 ? (int)GATransactionStatus.Success : (int)GATransactionStatus.Fail;
        }

    }
}
