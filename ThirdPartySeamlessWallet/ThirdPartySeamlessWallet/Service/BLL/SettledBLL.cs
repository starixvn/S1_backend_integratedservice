using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class SettledBLL
    {
        private readonly SettledService settledService;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;

        public SettledBLL(SettledService settledService, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL)
        {
            this.settledService = settledService;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.thirdPartyBLL = thirdPartyBLL;
        }

        public int Refund(int tpId, string TransId, string tpUserName, decimal backAmount, long SettledTime, GameType gameType, out decimal tpAfterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            //tpCustBalance.ExchangeRate
            BetRecordInfo betRecordInfo = new BetRecordInfo();
            if (tpCurrency.ExchangeRate > 0)
            {
                backAmount *= tpCurrency.ExchangeRate;
            }
            betRecordInfo.Stake = backAmount;
            betRecordInfo.TpId = tpId;
            betRecordInfo.TransId = TransId;
            betRecordInfo.GameType = gameType;
            betRecordInfo.TpUserName = tpUserName;
            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
            betRecordInfo.SettledTime = SettledTime;
            betRecordInfo.TicketStatus = TicketStatus.Refund;

            var result = settledService.SettleTicket(tpCustInfo, betRecordInfo, backAmount, out tpAfterAmount);
            if (result > 0 && tpAfterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                tpAfterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int Win(int tpId, string TransId, string tpUserName, decimal wonAmount, long SettledTime, GameType gameType, out decimal tpAfterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            //tpCustBalance.ExchangeRate
            BetRecordInfo betRecordInfo = new BetRecordInfo();
            if (tpCurrency.ExchangeRate > 0)
            {
                wonAmount *= tpCurrency.ExchangeRate;
            }

            betRecordInfo.TpId = tpId;
            betRecordInfo.TransId = TransId;
            betRecordInfo.GameType = gameType;
            betRecordInfo.TpUserName = tpUserName;
            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
            betRecordInfo.SettledTime = SettledTime;
            betRecordInfo.TicketStatus = TicketStatus.Win;

            var result = settledService.SettleTicket(tpCustInfo, betRecordInfo, wonAmount, out tpAfterAmount);
            if (result > 0 && tpAfterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                tpAfterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int Lost(int tpId, string TransId, string tpUserName, long SettledTime, GameType gameType, out decimal tpAfterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            //tpCustBalance.ExchangeRate
            BetRecordInfo betRecordInfo = new BetRecordInfo();
            betRecordInfo.TpId = tpId;
            betRecordInfo.TransId = TransId;
            betRecordInfo.GameType = gameType;
            betRecordInfo.TpUserName = tpUserName;
            betRecordInfo.SettledTime = SettledTime;
            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
            betRecordInfo.TicketStatus = TicketStatus.Lost;

            var result = settledService.SettleTicket(tpCustInfo, betRecordInfo, 0, out tpAfterAmount);
            if (result > 0 && tpAfterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                tpAfterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int SettledLC_AG(PayoutDataLC_AG payoutDataLC_AG, out decimal afterAmount)
        {
            string username = payoutDataLC_AG.Playname.Substring(payoutDataLC_AG.AgentCode.Length);
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AG, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutDataLC_AG.NetAmount *= tpCurrency.ExchangeRate;
                payoutDataLC_AG.ValidBetAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledLC_AG(tpCustInfo, payoutDataLC_AG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int SettledLC_AG_Merger(PayoutDataLC_AG payoutDataLC_AG, string relTransId)
        {
            int result = settledService.SettledLC_AG_Merger(payoutDataLC_AG, relTransId);
            return result;
        }
    }
}
