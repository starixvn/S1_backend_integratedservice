using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using Serilog.Context;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public partial class SettleWorker : BackgroundService
    {
        #region Fields

        private Timer timerSettleAG;
        private Timer timerSettleDG;
        private Timer timerSettleEBet;
        private Timer timerSettleWM;
        
        private Timer timerSettleCQ9Fish;
      
        private Timer timerSettleAWC;
        private Timer timerSettleYGG;
        
        private Timer timerSettleOne789LT;
        private Timer timerSettleOne789LT_789;
        private Timer timerSettleOne789LC;
        private Timer timerSettleYLFish;
        private Timer timerSettleN2Live;
        private Timer timerSettleN2LiveLC_Smart;
        private Timer timerSettleBTI;
        private Timer timerSettle9W;
        private Timer timerSettleTCGSEA;
        private Timer timerSettleTCGVN;
        private Timer timerSettleSexy;
        private Timer timerSettleEVO;
        private Timer timerSettleRewardEVO;
     
      

        private readonly IConfiguration configuration;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private readonly SettledBLL settledBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ILogger<SettleWorker> _logger;
        private readonly TpUnSettledBLL _tpUnSettledBLL;

        #endregion

        public SettleWorker(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            environment = provider.GetRequiredService<IHostEnvironment>();
            log2File = provider.GetRequiredService<Log2File>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<SettleWorker>>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettleAG = new Timer(DoSettleAGWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleDG = new Timer(DoSettleDGWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleEBet = new Timer(DoSettleEBetWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleWM = new Timer(DoSettleWMWork, null, TimeSpan.Zero, TimeSpan.Zero);
            
            timerSettleCQ9Fish = new Timer(DoSettleCQ9FishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            //timerSettleAWC = new Timer(DoSettleAWCWork, null, TimeSpan.Zero, TimeSpan.Zero);
           
            timerSettleOne789LT = new Timer(DoSettleOne789LTWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LT_789 = new Timer(DoSettleOne789LT_789Work, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LC = new Timer(DoSettleOne789LCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleYLFish = new Timer(DoSettleYLFishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleN2Live = new Timer(DoSettleN2LiveWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleN2LiveLC_Smart = new Timer(DoSettleN2LiveLC_SmartWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleBTI = new Timer(DoSettleBTIWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettle9W = new Timer(DoSettle9WWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleSexy = new Timer(DoSettleSexyWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleEVO = new Timer(DoSettleEVOWork, null, TimeSpan.Zero, TimeSpan.Zero);
            
           
            timerSettleRewardEVO = new Timer(DoSettleRewardEVOWork, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }

        #region Methods

        private void DoSettleAGWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            const int thisTp = (int)Tp.AG;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_AG();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var payoutList = settledBLL.Get_PayoutList_AG(unsettled.TransactionID);
                            var betList = new List<BettingDataLC_AG>();
                            List<string> transactionIDs = new List<string>();
                            if (payoutList != null)
                            {
                                foreach (var payout in payoutList)
                                {
                                    transactionIDs.AddRange(JsonConvert.DeserializeObject<IEnumerable<string>>(payout.TransactionIDs));
                                }

                                var bBetList = settledBLL.Get_BetList_AG(JsonConvert.SerializeObject(transactionIDs.Distinct()));

                                if (bBetList != null)
                                {
                                    betList.AddRange(bBetList);
                                }

                                if (betList.Count > 0 && betList.Sum(x => x.Value) == payoutList.Sum(x => x.ValidBetAmount))
                                {
                                    var billNoList = payoutList.Select(x => x.BillNo).ToArray();
                                    var wager = settledBLL.GetSettle_TpWagerAG(JsonConvert.SerializeObject(billNoList));

                                    if (wager != null)
                                    {
                                        string username = unsettled.Playname.Substring(unsettled.AgentCode.Length);

                                        var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AG, username);
                                        var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

                                        if (tpCurrency.ExchangeRate > 1)
                                        {
                                            wager.Stake *= tpCurrency.ExchangeRate;
                                            wager.ValidBet *= tpCurrency.ExchangeRate;
                                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                        }

                                        if (betList.Sum(x => x.Value) == wager.Stake)
                                        {
                                            BetRecordInfo betRecordInfo = new BetRecordInfo
                                            {
                                                TpId = (int)Tp.AG,
                                                Stake = wager.Stake,
                                                ValidBet = wager.ValidBet,
                                                WinlostAmount = wager.WinLostAmount,
                                                TransId = unsettled.TransactionID,
                                                GameType = GameType.AG_LiveCasino,
                                                TpUserName = username,
                                                SettledTime = wager.SettleTime,
                                                TransTime = wager.TransTime,
                                                SettlementType = SettlementType.SettledForValid
                                            };

                                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                            Task.Run(() =>
                                            {
                                                settledBLL.SetSettleFlag_TpWagerAG(JsonConvert.SerializeObject(billNoList));
                                                settledBLL.SetSettleFlag_AG(unsettled.TransactionID, JsonConvert.SerializeObject(transactionIDs));
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleAG", ex1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleAG", ex);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleAG.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleDGWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.DG;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_DG();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var wager = settledBLL.GetSettle_TpWagerDG(unsettled.TransId);

                            if (wager != null)
                            {
                                //会员发红包、会员抢红包、主播小费、公司发红包、博饼记录、荷官小费
                                if (wager.GameType == 2)
                                {
                                    Task.Run(() =>
                                    {
                                        settledBLL.SetSettleFlag_TpWagerDG(unsettled.TransId);
                                        settledBLL.SetSettleFlag_DG(unsettled.TransId);
                                    });
                                    continue;
                                }

                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Username);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);

                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    wager.Stake *= tpCurrency.ExchangeRate;
                                    wager.ValidBet *= tpCurrency.ExchangeRate;
                                    wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                }

                                BetRecordInfo betRecordInfo = new BetRecordInfo
                                {
                                    TpId = thisTp,
                                    Stake = wager.Stake,
                                    ValidBet = wager.ValidBet,
                                    WinlostAmount = wager.WinLostAmount,
                                    TransId = unsettled.TransId,
                                    GameType = GameType.DG_LiveCasino,
                                    TpUserName = unsettled.Username,
                                    SettledTime = wager.SettleTime,
                                    TransTime = wager.TransTime,
                                    SettlementType = SettlementType.SettledForValid
                                };

                                var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                Task.Run(() =>
                                {
                                    settledBLL.SetSettleFlag_TpWagerDG(unsettled.TransId);
                                    settledBLL.SetSettleFlag_DG(unsettled.TransId);
                                });
                            }

                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleDG");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleDG");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleDG.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleEBetWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.EBet;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_EBet();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = GameType.EBet_LiveCasino,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime / 1000,
                                TransTime = unsettled.TransTime / 1000,
                                SettlementType = SettlementType.SettledForValid
                            };
                            // check refund
                            if (unsettled.TicketStatus > 0)
                            {
                                betRecordInfo.TicketStatus = TicketStatus.Refund;
                            }
                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                            Task.Run(() =>
                            {
                                settledBLL.SetSettleFlag_EBet(unsettled.TransId);
                            });
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleEBet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleEBet");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleEBet.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleWMWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.WM;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_WM();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        var wager = settledBLL.GetSettle_TpWagerWM(unsettled.TransId);
                        if (wager != null)
                        {
                            try
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, wager.Account);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    wager.Stake *= tpCurrency.ExchangeRate;
                                    wager.ValidBet *= tpCurrency.ExchangeRate;
                                    wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                }
                                if (wager.Stake == unsettled.RealBetAmount && wager.WinLostAmount == unsettled.WinLostAmount)
                                {
                                    BetRecordInfo betRecordInfo = new BetRecordInfo
                                    {
                                        TpId = thisTp,
                                        Stake = wager.Stake,
                                        ValidBet = wager.ValidBet,
                                        WinlostAmount = wager.WinLostAmount,
                                        TransId = wager.TransId,
                                        GameType = GameType.WM_LiveCasino,
                                        TpUserName = wager.Account,
                                        SettledTime = wager.SettleTime,
                                        TransTime = wager.TransTime,
                                        SettlementType = SettlementType.SettledForValid
                                    };

                                    var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                    settledBLL.SetSettleFlag_TpWagerWM(unsettled.TransId);
                                    settledBLL.SetSettleFlag_BettransLC_WM(unsettled.TransId);
                                }

                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", tpName);
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleWM");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleWM");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleWM.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleOne789LCWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.One789;
            var gameType = (int)GameType.One789_LiveCasino;
            string logName = "Settle_One789_LC";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_One789(gameType);

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        var wager = settledBLL.GetSettle_TpWagerOne789(gameType, unsettled.TransId);
                        if (wager != null)
                        {
                            try
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, wager.Account);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    wager.Stake *= tpCurrency.ExchangeRate;
                                    wager.ValidBet *= tpCurrency.ExchangeRate;
                                    wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                }
                                if (wager.Stake == unsettled.Stake && wager.WinLostAmount == unsettled.WinLostAmount)
                                {
                                    BetRecordInfo betRecordInfo = new BetRecordInfo
                                    {
                                        TpId = thisTp,
                                        Stake = wager.Stake,
                                        ValidBet = wager.ValidBet,
                                        WinlostAmount = wager.WinLostAmount,
                                        TransId = wager.TransId,
                                        GameType = (GameType)gameType,
                                        TpUserName = wager.Account,
                                        SettledTime = wager.SettleTime,
                                        TransTime = wager.TransTime,
                                        SettlementType = SettlementType.SettledForValid
                                    };

                                    var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                    settledBLL.SetSettleFlag_TpWagerOne789(gameType, unsettled.TransId);
                                    settledBLL.SetSettleFlag_Bettrans_One789(gameType, unsettled.TransId);
                                }

                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", tpName);
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleOne789LC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleN2LiveWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.N2Live;
            var gameType = (int)GameType.N2Live_LiveCasino;
            string logName = "Settle_N2Live";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_BettransLC_N2Live();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        string TransId = unsettled.TransId;
                        var wager = settledBLL.GetSettle_TpWagerLC_N2Live(TransId);
                        if (wager != null)
                        {
                            try
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    wager.Stake *= tpCurrency.ExchangeRate;
                                    wager.ValidBet *= tpCurrency.ExchangeRate;
                                    wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                }
                                if (wager.Stake == unsettled.Stake && wager.WinLostAmount == unsettled.WinLostAmount)
                                {
                                    BetRecordInfo betRecordInfo = new BetRecordInfo
                                    {
                                        TpId = thisTp,
                                        Stake = wager.Stake,
                                        ValidBet = wager.ValidBet,
                                        WinlostAmount = wager.WinLostAmount,
                                        TransId = unsettled.TransId,
                                        GameType = (GameType)gameType,
                                        TpUserName = unsettled.Account,
                                        SettledTime = wager.SettleTime,
                                        TransTime = wager.TransTime,
                                        SettlementType = SettlementType.SettledForValid,
                                        ReferenceNo = wager.betid // for fix cant mapping bonusid when data fixed can remove
                                    };

                                    var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                    settledBLL.SetSettleFlag_TpWagerLC_N2Live(TransId);
                                    settledBLL.SetSettleFlag_BettransLC_N2Live(TransId, 1);
                                }
                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", tpName);
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                            }
                        }
                        else
                        {
                            // smart and player bet
                            var smart = settledBLL.GetSettle_TpSmartWagerLC_N2Live(TransId);
                            if (smart != null)
                            {
                                settledBLL.SetSettleFlag_BettransLC_N2Live(TransId, 0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleN2Live.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleN2LiveLC_SmartWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.N2Live;
            var gameType = (int)GameType.N2Live_LiveCasino;
            string logName = "Settle_N2Live_LC_Smart";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_TpSmartWagerLC_N2Live();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            string TransId = unsettled.TransId;
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.ValidBet *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = (GameType)gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                ReferenceNo = unsettled.schemeid // for smart bet mapping
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                            settledBLL.SetSettleFlag_TpSmartWagerLC_N2Live(TransId);

                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleN2LiveLC_Smart.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleSexyWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Sexy;
            var gameType = GameType.Sexy_LiveCasino;
            var category = TpCategory.LiveCasino;
            string logName = "Settle_AWC_Sexy";
            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_AWC_Sexy();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = unsettled.TicketStatus == 0 ? SettlementType.UnSettled : SettlementType.SettledForValid,
                                TicketStatus = (TicketStatus)unsettled.TicketStatus
                            };

                            settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_BettransLC_AWC_Sexy(unsettled.TransId);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleSexy.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleEVOWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.EVO;
            var gameType = GameType.EVO_LiveCasino;
            var category = TpCategory.LiveCasino;
            string logName = "Settle_EVO_LC";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_EVO();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        string TransId = unsettled.TransId;
                        var wager = settledBLL.GetSettle_TpWagerLC_EVO(TransId);

                        if (wager != null)
                        {
                            try
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    wager.Stake *= tpCurrency.ExchangeRate;
                                    wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                }
                                decimal stake = unsettled.Stake;
                                decimal validBet = unsettled.Stake;
                                decimal winLostAmount = unsettled.WinLostAmount;

                                if (wager.Stake == stake)
                                {
                                    BetRecordInfo betRecordInfo = new BetRecordInfo
                                    {
                                        TpId = thisTp,
                                        Stake = stake,
                                        ValidBet = validBet,
                                        WinlostAmount = winLostAmount,
                                        TransId = unsettled.TransId.ToString(),
                                        GameType = gameType,
                                        TpUserName = unsettled.Account,
                                        SettledTime = wager.SettleTime,
                                        TransTime = wager.TransTime,
                                        SettlementType = SettlementType.SettledForValid,
                                    };

                                    if (betRecordInfo.WinlostAmount == 0)
                                    {
                                        betRecordInfo.TicketStatus = TicketStatus.Draw;
                                    }

                                    settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                                    settledBLL.SetSettleFlag_EVO(unsettled.TransId);
                                }
                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", tpName);
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleEVO.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleRewardEVOWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            string logName = "Settle_Reward_EVO";
            var tpName = string.Empty;

            try
            {
                var unsettledList = settledBLL.GetUnsettledRewardList_EVO();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        tpName = Enum.GetName(typeof(Tp), unsettled.TpId);
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(unsettled.TpId, unsettled.userId);
                            var tpCurrency = thirdPartyBLL.GetTpCurrency(unsettled.TpId, tpCustInfo.CurrencyId);

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = unsettled.TpId,
                                GameType = (GameType)unsettled.TpGameType,
                                TpUserName = unsettled.userId,
                                Stake = 0,
                                ValidBet = 0,
                                WinlostAmount = unsettled.PromoTransactionAmount,
                                TransId = unsettled.PromoTransactionId,
                                SettledTime = unsettled.CreateTime,
                                TransTime = unsettled.CreateTime,
                                SettlementType = SettlementType.SettledForValid,
                            };

                            var tpCategory = unsettled.TpId == (int)Tp.EVO ? TpCategory.LiveCasino : TpCategory.Slots;
                            settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                            settledBLL.SetRewardSettleFlag_EVO(unsettled.SQ);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"{logName}：Settle Fail. TpName={tpName}. data={JsonConvert.SerializeObject(unsettled)}. exception={ex.Message}");
                            isError = true;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logName}：Worker Exception. TpName={tpName}. exception={ex.Message}");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleRewardEVO.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        #endregion

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timerSettleAG?.Change(Timeout.Infinite, 0);
            timerSettleDG?.Change(Timeout.Infinite, 0);
            timerSettleEBet?.Change(Timeout.Infinite, 0);
            timerSettleWM?.Change(Timeout.Infinite, 0);
            
            timerSettleCQ9Fish?.Change(Timeout.Infinite, 0);
            
            timerSettleAWC?.Change(Timeout.Infinite, 0);
            
            timerSettleOne789LT?.Change(Timeout.Infinite, 0);
            timerSettleOne789LT_789?.Change(Timeout.Infinite, 0);
            timerSettleOne789LC?.Change(Timeout.Infinite, 0);
            timerSettleYGG?.Change(Timeout.Infinite, 0);
            timerSettleYLFish?.Change(Timeout.Infinite, 0);
            timerSettleN2Live?.Change(Timeout.Infinite, 0);
            timerSettleN2LiveLC_Smart?.Change(Timeout.Infinite, 0);
            timerSettleBTI?.Change(Timeout.Infinite, 0);
            timerSettle9W?.Change(Timeout.Infinite, 0);
            timerSettleTCGSEA?.Change(Timeout.Infinite, 0);
            timerSettleTCGVN?.Change(Timeout.Infinite, 0);
            timerSettleSexy?.Change(Timeout.Infinite, 0);
            timerSettleEVO?.Change(Timeout.Infinite, 0);
            timerSettleRewardEVO?.Change(Timeout.Infinite, 0);
           
          

            return Task.CompletedTask;
        }
    }
}
