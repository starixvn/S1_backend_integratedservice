using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.KY;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class KYBLL
    {
        #region Response

        public class GetApiResp
        {
            public int s { get; set; }
            public string m { get; set; }
            public info d { get; set; }
        }

        public class info
        {
            public int code { get; set; }
        }

        public class GetBalanceRespOfSeamless
        {
            public int s { get; set; }
            public string m { get; set; }
            public GetBalanceD d { get; set; }
        }

        public class PlaceBetResp : GetBalanceRespOfSeamless
        {
        }

        public class PayoutResp : GetBalanceRespOfSeamless
        {
        }

        public class GetOrderStatusResp
        {
            public int s { get; set; }
            public string m { get; set; }
            public GetOrderStatusD d { get; set; }
        }

        public class CancelBetResp : GetOrderStatusResp
        {
        }

        public class GetBalanceD
        {
            public int code { get; set; }
            public string account { get; set; }
            public decimal money { get; set; }
        }

        public class GetOrderStatusD
        {
            public int code { get; set; }
            public int status { get; set; }
        }

        #endregion

        private readonly int _thisTp;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly Log2File _log2File;
        private readonly int _gameType = (int)GameType.KY_Poker;

        public KYBLL(IServiceProvider provider)
        {
            _thisTp = (int)Tp.KY;
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        #region Api Verify & Process

        public dynamic PlaceBet(ApiReq getReq, string reqParam)
        {
            var result = 0;
            string logName = $"{_thisTp}_PlaceBet";
            var subType = SubTypeOfKY.RequestBet;
            GetApiResp getApiResponse = new GetApiResp
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<BettingDataPK_KY>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, req.account);
                if (tpCheck == null)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Account;
                    _log2File.Error($"{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Locked)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Account;
                    _log2File.Error($"Locked {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = PlaceBetPK_KY(req, out decimal tpAfterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAfterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, req.account, _gameType);
                        tpAfterAmount = tpCustBalance.Balance;
                    }

                    PlaceBetResp getPlaceBetResponse = new()
                    {
                        s = (int)subType,
                        m = "/channelHandle",
                        d = new GetBalanceD
                        {
                            code = (int)ErrorCodeOfKY.Success,
                            account = req.account,
                            money = tpAfterAmount
                        }
                    };
                    return getPlaceBetResponse;
                }
                else if (tpAfterAmount < 0)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Insufficient;
                    _log2File.Error($"{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error result={result} {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        public dynamic Payout(ApiReq getReq, string reqParam)
        {
            var result = 0;
            string logName = $"{_thisTp}_Payout";
            var subType = SubTypeOfKY.ReturnBalance;
            GetApiResp getApiResponse = new GetApiResp
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<BettingDataPK_KY>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = PayoutPK_KY(req, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, req.account, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    PayoutResp getPayoutResponse = new()
                    {
                        s = (int)subType,
                        m = "/channelHandle",
                        d = new GetBalanceD
                        {
                            code = (int)ErrorCodeOfKY.Success,
                            account = req.account,
                            money = tpAffterAmount
                        }
                    };
                    return getPayoutResponse;
                }
                else if (result == -2)
                {
                    _log2File.Error("no bet", logName);
                }
                else if (result == -3)
                {
                    _log2File.Error("duplicate payout", logName);
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error result={result} {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        public dynamic CancelBet(ApiReq getReq, string reqParam)
        {
            var result = 0;
            string logName = $"{_thisTp}_CancelBet";
            var subType = SubTypeOfKY.CancelBet;
            CancelBetResp getApiResponse = new()
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new GetOrderStatusD
                {
                    code = 999,
                    status = (int)StatusTypeOfKY.Failure
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<BettingDataPK_KY>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = CancelBetPK_KY(req, out decimal tpAfterAmount);
                if (result > 0 || result == -1)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Success;
                    getApiResponse.d.status = (int)StatusTypeOfKY.Success;
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error result={result} {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        #endregion

        #region ExchangeRate & Service

        private int PlaceBetPK_KY(BettingDataPK_KY bettingDataPK_KY, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_KY.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_KY.money *= tpCurrency.ExchangeRate;
            }

            var result = _bettingService.PlaceBetPK_KY(tpCustInfo, bettingDataPK_KY, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int PayoutPK_KY(BettingDataPK_KY bettingDataPK_KY, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_KY.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_KY.money *= tpCurrency.ExchangeRate;
            }

            var result = _settledService.PayoutPK_KY(tpCustInfo, bettingDataPK_KY, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int CancelBetPK_KY(BettingDataPK_KY bettingDataPK_KY, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_KY.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_KY.money *= tpCurrency.ExchangeRate;
            }

            var result = _settledService.CancelBetPK_KY(tpCustInfo, bettingDataPK_KY, out afterAmount );
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion
    }
}
