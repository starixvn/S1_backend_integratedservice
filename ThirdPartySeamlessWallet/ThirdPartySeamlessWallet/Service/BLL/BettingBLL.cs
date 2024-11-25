using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public partial class BettingBLL
    {
        private readonly ILogger<BettingBLL> _logger;
        private readonly BettingService bettingService;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;

        public BettingBLL(BettingService bettingService, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, WalletService walletService, ILogger<BettingBLL> logger)
        {
            this.bettingService = bettingService;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            _logger = logger;
        }


        public int PlaceBetLC_AG(BettingDataLC_AG bettingDataLC_AG, out decimal afterAmount)
        {
            string username = bettingDataLC_AG.Playname.Substring(bettingDataLC_AG.AgentCode.Length);
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AG, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_AG.Value *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLC_AG(tpCustInfo, bettingDataLC_AG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetLC_SA(BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.SA, bettingDataLC_SA.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SA, tpCustInfo.CurrencyId);

            if (bettingDataLC_SA.Betdetails == null)
            {
                bettingDataLC_SA.BetAmount = bettingDataLC_SA.amount;
            }
            else
            {
                bettingDataLC_SA.BetAmount = JsonConvert
                    .DeserializeObject<BettingDataLC_SA_BetDetail>(bettingDataLC_SA.Betdetails).details
                    .Sum(i => i.amount);
            }

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_SA.amount *= tpCurrency.ExchangeRate; /*bettingDataLC_SA.Betdetails has not fixed*/
                bettingDataLC_SA.BetAmount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLC_SA(tpCustInfo, bettingDataLC_SA, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int PlaceBetLC_DG(BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.DG, bettingDataLC_DG.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.DG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_DG.Amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLC_DG(tpCustInfo, bettingDataLC_DG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetRNG_GPI(BettingDataRNG_GPI bettingDataRNG_GPI, out decimal afterAmount, out string TransId)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.GPI, bettingDataRNG_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_GPI.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_GPI(tpCustInfo, bettingDataRNG_GPI, out afterAmount, out TransId);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int PlaceBetRNG_Joker(BettingDataRNG_Joker bettingDataRNG_Joker, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Joker, bettingDataRNG_Joker.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Joker, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_Joker.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_Joker(tpCustInfo, bettingDataRNG_Joker, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_Joker.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int PlaceBetRNG_YGG(BettingDataRNG_YGG bettingDataRNG_YGG, out decimal afterAmount, out string countryCode)
        {
            var result = 0;
            afterAmount = 0;
            countryCode = string.Empty;

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.YGG, bettingDataRNG_YGG.playerid);

            if (tpCustInfo != null)
            {
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YGG, tpCustInfo.CurrencyId);
                var tpCountry = tpCustInfoBLL.GetTpCurrencyCodeByCurrencyId((int)Tp.YGG, tpCustInfo.TpCurrencyId);

                if (tpCountry != null)
                {
                    countryCode = tpCountry.CountryCode;
                }

                var insertData = new BettingDataRNG_YGG()
                {
                    playerid = bettingDataRNG_YGG.playerid,
                    amount = tpCurrency.ExchangeRate > 0 ? bettingDataRNG_YGG.amount * tpCurrency.ExchangeRate * -1 : bettingDataRNG_YGG.amount,
                    currency = bettingDataRNG_YGG.currency,
                    reference = bettingDataRNG_YGG.reference,
                    subreference = bettingDataRNG_YGG.subreference,
                    description = bettingDataRNG_YGG.description,
                    cat1 = bettingDataRNG_YGG.cat1,
                    cat2 = bettingDataRNG_YGG.cat2,
                    cat3 = bettingDataRNG_YGG.cat3,
                    cat4 = bettingDataRNG_YGG.cat4,
                    cat5 = bettingDataRNG_YGG.cat5,
                    tag1 = bettingDataRNG_YGG.tag1,
                    tag2 = bettingDataRNG_YGG.tag2,
                    tag3 = bettingDataRNG_YGG.tag3,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettlementType.UnSettled
                };

                result = bettingService.PlaceBetRNG_YGG(tpCustInfo, insertData, out afterAmount);

                if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                {
                    afterAmount /= tpCurrency.ExchangeRate;
                }
            }

            return result;
        }
        public int PlaceBetRNG_SG(BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.SG, bettingDataRNG_SG.acctId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_SG.amount *= tpCurrency.ExchangeRate;
            }
            var result = bettingService.PlaceBetRNG_SG(tpCustInfo, bettingDataRNG_SG, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_PP(tpCustInfo, bettingDataRNG_PP, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetRNG_PG(BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PG, bettingDataRNG_PG.PlayerName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PG.TransferAmount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_PG(tpCustInfo, bettingDataRNG_PG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #region AESexy

        public int PlaceBetLC_Sexy(BettingDataLC_Sexy_Bet bettingDataLC_Sexy_Bet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AESexy, bettingDataLC_Sexy_Bet.UserId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AESexy, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                foreach (var txn in bettingDataLC_Sexy_Bet.Txns)
                {
                    txn.BetAmount *= tpCurrency.ExchangeRate;
                }
            }

            var result = bettingService.PlaceBetLC_Sexy(tpCustInfo, bettingDataLC_Sexy_Bet, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetLC_AWC_Sexy(BetLC_Sexy betLC_Sexy, out decimal afterAmount)
        {
            afterAmount = 0;
            var userId = betLC_Sexy.txns.ElementAt(0).userId;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AESexy, userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AESexy, tpCustInfo.CurrencyId);
            var custBalance = tpCustInfoBLL.GetBalance((int)Tp.AESexy, userId, (int)GameType.AESexy_LiveCasino);
            if (tpCurrency.ExchangeRate > 0)
            {
                custBalance.Balance *= tpCurrency.ExchangeRate;
            }
            foreach (var item in betLC_Sexy.txns)
            {
                if (item.userId != userId)
                {
                    return -99;
                }
                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                }
            }

            // check
            var totalBetAmount = betLC_Sexy.txns.Sum(x => x.betAmount);
            var checkBalance = custBalance.Balance - Math.Abs(totalBetAmount);
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                return 0;
            }
            var result = bettingService.PlaceBetLC_AWC_Sexy(tpCustInfo, betLC_Sexy, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int TipLC_AWC_Sexy(TipLC_Sexy tipLC_Sexy, out decimal afterAmount)
        {
            afterAmount = 0;
            var userId = tipLC_Sexy.txns.ElementAt(0).userId;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AESexy, userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AESexy, tpCustInfo.CurrencyId);
            foreach (var item in tipLC_Sexy.txns)
            {
                if (item.userId != userId)
                {
                    return -99;
                }
                if (tpCurrency.ExchangeRate > 0)
                {
                    item.tip *= tpCurrency.ExchangeRate;
                }
            }

            var result = bettingService.TipLC_AWC_Sexy(tpCustInfo, tipLC_Sexy, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        public int PlaceBetLC_EBet(BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.WE, bettingData_EBet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.WE, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.totalBet *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.validBet *= tpCurrency.ExchangeRate;
                foreach (var item in bettingData_EBet.detail.betList)
                {
                    item.betMoney *= tpCurrency.ExchangeRate;
                }
            }

            var result = bettingService.PlaceBetLC_EBet(tpCustInfo, bettingData_EBet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }
            else if (result == -1 && tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        #region CQ9

        public int PlaceBetRNG_CQ9(BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CQ9, bettingDataRNG_CQ9.account);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_CQ9(tpCustInfo, bettingDataRNG_CQ9, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int RolloutFish_CQ9(BettingDataRNG_CQ9 bettingDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, bettingDataRNG_CQ9.account);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.RolloutFish_CQ9(tpCustInfo, bettingDataRNG_CQ9, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        
        public int RolloutWCC_CQ9(BettingDataRNG_CQ9 bettingDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, bettingDataRNG_CQ9.account);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.RolloutWCC_CQ9(tpCustInfo, bettingDataRNG_CQ9, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int TakeAllFish_CQ9(BettingDataRNG_CQ9 bettingDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, bettingDataRNG_CQ9.account);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.TakeAllFish_CQ9(tpCustInfo, bettingDataRNG_CQ9, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        public int PlaceBetLC_WM(BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.WM, bettingDataLC_WM.user);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.WM, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_WM.money *= tpCurrency.ExchangeRate;
                bettingDataLC_WM.RealBetAmount = bettingDataLC_WM.DetailSumAmount * tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLC_WM(tpCustInfo, bettingDataLC_WM, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetRNG_Iconic(BettingDataRNG_Iconic bettingDataRNG_Iconic, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Iconic, bettingDataRNG_Iconic.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Iconic, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_Iconic.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_Iconic(tpCustInfo, bettingDataRNG_Iconic, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetFish_Iconic(BettingDataFish_Iconic bettingDataFish_Iconic, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Iconic, bettingDataFish_Iconic.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Iconic, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataFish_Iconic.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetFish_Iconic(tpCustInfo, bettingDataFish_Iconic, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetLC_N2Live(BettingDataLC_N2Live bettingDataLC_N2Live, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.N2Live, bettingDataLC_N2Live.request.userid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.N2Live, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_N2Live.request.totalamount *= tpCurrency.ExchangeRate;
                foreach (var item in bettingDataLC_N2Live.request.trades.trade)
                {
                    item.amount *= tpCurrency.ExchangeRate;
                    if (item.hand != null)
                    {
                        item.hand.text *= tpCurrency.ExchangeRate;
                    }
                }
            }
            var result = bettingService.PlaceBetLC_N2Live(tpCustInfo, bettingDataLC_N2Live, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int SmartPlaceBetLC_N2Live(SmartBettingDataLC_N2Live smartBettingDataLC_N2Live, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.N2Live, smartBettingDataLC_N2Live.request.userid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.N2Live, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                smartBettingDataLC_N2Live.request.amount *= tpCurrency.ExchangeRate;
            }
            var result = bettingService.SmartPlaceBetLC_N2Live(tpCustInfo, smartBettingDataLC_N2Live, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #region EVO

        public int PlaceBetLC_EVO(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EVO, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EVO, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLC_EVO(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region Netent

        public int PlaceBetRNG_Netent(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Netent, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Netent, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_Netent(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        #endregion

        #region RedTiger

        public int PlaceBetRNG_RedTiger(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.RedTiger, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.RedTiger, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_RedTiger(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region NLC

        public int PlaceBetRNG_NLC(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.NLC, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.NLC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_NLC(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region Joker Fish

        public int WithdrawFish_Joker(BettingDataFish_Joker bettingDataFish_Joker, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Joker, bettingDataFish_Joker.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Joker, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataFish_Joker.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.WithdrawFish_Joker(tpCustInfo, bettingDataFish_Joker, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataFish_Joker.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        #endregion

        #region AP
        public int PlaceBet_AP(BettingDataRNG_AP bettingData_AP, out decimal afterAmount, out long seq)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, bettingData_AP.playerid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_AP.stake *= tpCurrency.ExchangeRate;
            }
            var result = bettingService.PlaceBet_AP(tpCustInfo, bettingData_AP, out afterAmount, out seq);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBet_AdvantPlayMini(BettingData_AdvantPlayMini bettingData_AdvantPlayMini, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, bettingData_AdvantPlayMini.UserReferenceId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingData_AdvantPlayMini.Amount *= tpCurrency.ExchangeRate;
            }
            var result = bettingService.PlaceBet_AdvantPlayMini(tpCustInfo, bettingData_AdvantPlayMini, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<BettransData_AdvantPlayMini_Bet> GetAdvantPlayMiniBet(string TransId, long SettlementTime, string MiniGameCode, out string TransactionReferenceId)
        {
            return bettingService.GetAdvantPlayMiniBet(TransId, SettlementTime, MiniGameCode, out TransactionReferenceId);
        }

        public IEnumerable<BettransData_AdvantPlayMini_Bet> GetAdvantPlayMiniBySession(string GameId, long SessionId, long SettlementTime, string MiniGameCode, out string TransactionReferenceId)
        {
            return bettingService.GetAdvantPlayMiniBySession(GameId, SessionId, SettlementTime, MiniGameCode, out TransactionReferenceId);
        }
        #endregion

        #region JILI

        public int PlaceBetRNG_JILI(TpCustInfo tpCustInfo, BettingData_JILI bettingData_JILI, out decimal afterAmount)
        {
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.JILI, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate != 0)
            {
                bettingData_JILI.betAmount *= tpCurrency.ExchangeRate;
                bettingData_JILI.winloseAmount *= tpCurrency.ExchangeRate;
                bettingData_JILI.turnover *= tpCurrency.ExchangeRate;
                bettingData_JILI.preserve *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_JILI(tpCustInfo, bettingData_JILI, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region SimplePlay

        public int PlaceBetRNG_SimplePlay(BettingData_SimplePlay bettingData_SimplePlay, int gameType, out decimal afterAmount)
        {
            var tpId = (int)Tp.SimplePlay;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, bettingData_SimplePlay.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_SimplePlay.amount *= tpCurrency.ExchangeRate;
                bettingData_SimplePlay.JackpotContribution *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_SimplePlay(tpCustInfo, bettingData_SimplePlay, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region Skywind
        public int PlaceBet_Skywind(TpCustInfo tpCustInfo, BettingData_Skywind bettingData_Skywind, int gameType, out decimal afterAmount)
        {
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Skywind, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_Skywind.amount *= tpCurrency.ExchangeRate;
            }
            var result = bettingService.PlaceBet_Skywind(tpCustInfo, bettingData_Skywind, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        #endregion

        #region PlayTech

        public int PlaceBet_PlayTech(BettingData_PlayTech bettingData_PlayTech, GameType gameType, int partitionCheckDay, out decimal afterAmount)
        {
            var tpId = (int)Tp.PlayTech;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, bettingData_PlayTech.playerId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            var custBalance = tpCustInfoBLL.GetBalance(tpId, bettingData_PlayTech.playerId, (int)gameType);

            decimal totalBet = 0;
            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var data in bettingData_PlayTech.trans)
                {
                    data.amount *= tpCurrency.ExchangeRate;
                    data.jpc *= tpCurrency.ExchangeRate;
                    if (string.Equals(data.transType, "bet", StringComparison.OrdinalIgnoreCase))
                    {
                        totalBet += data.amount;
                    }
                }
            }

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = bettingService.GetTpBetFailureParam_PlayTech(tpCustInfo, custBalance, tpCurrency, bettingData_PlayTech, (int)gameType, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var checkBalance = custBalance.Balance - totalBet;
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = bettingService.GetTpBetFailureParam_PlayTech(tpCustInfo, custBalance, tpCurrency, bettingData_PlayTech, (int)gameType, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var result = bettingService.PlaceBet_PlayTech(tpCustInfo, bettingData_PlayTech, gameType, partitionCheckDay, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region Endorphina

        public int PlaceBetRNG_Endorphina(BettingData_Endorphina bettingData_Endorphina, int gameType, out decimal afterAmount)
        {
            var tpId = (int)Tp.Endorphina;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, bettingData_Endorphina.player);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingData_Endorphina.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_Endorphina(tpCustInfo, bettingData_Endorphina, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion
    }
}
