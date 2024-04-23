using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class V8BLL
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
        private readonly int _gameType = (int)GameType.V8_Poker;

        public V8BLL(IServiceProvider provider)
        {
            _thisTp = (int)Tp.V8;
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        #region Api Verify & Process

        public dynamic GetBalance(string reqParam)
        {
            string logName = $"{_thisTp}_getBalance";
            var subType = SubTypeOfV8.GetBalance;
            GetApiResp getApiResponse = new GetApiResp
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };

            var req = JsonConvert.DeserializeObject<GetBalanceP>(reqParam);

            if (req.s != (int)subType)
            {
                getApiResponse.d.code = (int)ErrorCodeOfV8.Subtype;
                _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            if (req.account == null)
            {
                getApiResponse.d.code = (int)ErrorCodeOfV8.Account;
                _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, req.account);
            if (tpCustBalance != null)
            {
                GetBalanceRespOfSeamless getBalanceResponse = new()
                {
                    s = (int)subType,
                    m = "/channelHandle",
                    d = new GetBalanceD
                    {
                        code = (int)ErrorCodeOfV8.Success,
                        account = req.account,
                        money = tpCustBalance.Balance
                    }
                };
                return getBalanceResponse;
            }

            _log2File.Error($"Unknow Error {JsonConvert.SerializeObject(req)} {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        public dynamic PlaceBet(ApiReq getReq, string reqParam)
        {
            var result = 0;
            string logName = $"{_thisTp}_PlaceBet";
            var subType = SubTypeOfV8.RequestBet;
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
                var req = JsonConvert.DeserializeObject<BettingDataPK_V8>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, req.account);
                if (tpCheck == null)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Account;
                    _log2File.Error($"{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Locked)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Account;
                    _log2File.Error($"Locked {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = PlaceBetPK_V8(req, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, req.account, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    PlaceBetResp getPlaceBetResponse = new()
                    {
                        s = (int)subType,
                        m = "/channelHandle",
                        d = new GetBalanceD
                        {
                            code = (int)ErrorCodeOfV8.Success,
                            account = req.account,
                            money = tpAffterAmount
                        }
                    };
                    return getPlaceBetResponse;
                }
                else if (tpAffterAmount < 0)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Insufficient;
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
            var subType = SubTypeOfV8.ReturnBalance;
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
                var req = JsonConvert.DeserializeObject<BettingDataPK_V8>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = PayoutPK_V8(req, out decimal tpAffterAmount);
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
                            code = (int)ErrorCodeOfV8.Success,
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
            var subType = SubTypeOfV8.CancelBet;
            CancelBetResp getApiResponse = new()
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new GetOrderStatusD
                {
                    code = 999,
                    status = (int)StatusTypeOfV8.Failure
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<BettingDataPK_V8>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = CancelBetPK_V8(req, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Success;
                    getApiResponse.d.status = (int)StatusTypeOfV8.Success;
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

        public dynamic GetOrderStatus(ApiReq getReq, string reqParam)
        {
            string logName = $"{_thisTp}_GetOrderStatus";
            var subType = SubTypeOfV8.GetOrderStatus;
            GetOrderStatusResp getApiResponse = new()
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new GetOrderStatusD
                {
                    code = 999,
                    status = (int)StatusTypeOfV8.OtherError,
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<OrderDataPK_V8>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var bettrans = GetRecordPK_V8(req);
                if (!bettrans.Any() || bettrans == null)
                {
                    // 4:查无订单
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Success;
                    getApiResponse.d.status = (int)StatusTypeOfV8.NoResult;
                    return getApiResponse;
                }
                else if (bettrans.Any())
                {
                    // 1:成功
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Success;
                    getApiResponse.d.status = (int)StatusTypeOfV8.Success;
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        public BettransDataPK_V8 GetBettransByOrderId(OrderDataPK_V8 orderData)
        {
            // orderId 只會取出一筆
            var bettransData = GetRecordPK_V8(orderData);
            if (!bettransData.Any() || bettransData == null)
            {
                return null;
            }

            // 檢查是否為下注狀態資料
            var bettrans = bettransData.First();
            if (bettrans.TicketType != BettransType.Bet)
            {
                return null;
            }

            // 匯率還原
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettrans.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate != 0)
            {
                bettrans.money /= tpCurrency.ExchangeRate;
            }

            return bettrans;
        }

        #endregion

        #region ExchangeRate & Service

        private int PlaceBetPK_V8(BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_V8.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_V8.money *= tpCurrency.ExchangeRate;
            }

            var result = _bettingService.PlaceBetPK_V8(tpCustInfo, bettingDataPK_V8, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int PayoutPK_V8(BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_V8.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_V8.money *= tpCurrency.ExchangeRate;
            }

            var result = _settledService.PayoutPK_V8(tpCustInfo, bettingDataPK_V8, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int CancelBetPK_V8(BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_V8.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_V8.money *= tpCurrency.ExchangeRate;
            }

            var result = _settledService.CancelBetPK_V8(tpCustInfo, bettingDataPK_V8, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private IEnumerable<BettransDataPK_V8> GetRecordPK_V8(OrderDataPK_V8 orderDataPK_V8)
        {
            return _settledService.GetRecordPK_V8(orderDataPK_V8);
        }

        #endregion
    }
}
