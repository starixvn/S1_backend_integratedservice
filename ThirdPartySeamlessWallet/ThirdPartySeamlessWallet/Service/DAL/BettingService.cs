using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly Tools tools;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly ThirdPartyService thirdPartyService;

        public BettingService(IConfiguration configuration, WalletService walletService, ThirdPartyBLL thirdPartyBLL, Tools tools, ThirdPartyService thirdPartyService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.tools = tools;
            this.thirdPartyBLL = thirdPartyBLL;
            this.thirdPartyService = thirdPartyService;
        }
        private int AddTpBonus(int tpId, string transId, int gameType, int custId, string userName, int currencyId, int siteId, decimal stake, int bonusId, TpCategory tpCategory, MySqlConnection conn, MySqlTransaction trans)
        {
            if (bonusId == 0)
                return 0;
            int result = conn.Execute($"Service_Tp{tpCategory}Bonus_Ins", new { TpId = tpId, TransId = transId, GameType = gameType, CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Stake = stake, BonusId = bonusId }, transaction: trans, commandType: CommandType.StoredProcedure);

            return result;
        }
        private int PlaceBetV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, string refId, string betData, TpCategory tpCategory, out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            afterAmount = 0;
            int result = 0;
            bool bHasBonusWallet = true;

            if (bHasBonusWallet)
            {
                result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, ref bonusId, conn, trans);
                if (bonusId > 0)
                {
                    var bonusMinBet = thirdPartyBLL.GetBonusMinBet(gameType, tpCustInfo.CurrencyId);
                    if (afterAmount >= 0 && bonusMinBet?.MinBet > (Math.Abs(amount) + afterAmount))
                    {
                        bonusId = 0;
                    }
                    else if (afterAmount < 0 && bonusMinBet?.MinBet > (Math.Abs(amount) - Math.Abs(afterAmount)))
                    {
                        bonusId = 0;
                    }
                    else
                    {
                        if (tpTransId != null)
                        {
                            AddTpBonus(tpId, tpTransId, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, amount, bonusId, tpCategory, conn, trans);
                        }
                    }

                    if (afterAmount < 0)
                    {
                        amount = afterAmount;
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                    }
                    else
                    {
                        afterAmount += walletService.GetBalance(tpCustInfo.CustId);
                    }
                }
                else if (bonusId == 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                }
            }
            else
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
            }

            return result;
        }


    }
}
