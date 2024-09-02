using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class WMController : ControllerBase
    {
        private readonly Log2File _log2File;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly IHostEnvironment _environment;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;
        private readonly Dictionary<int, WMConfig> _siteConfigDic;

        public WMController(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _thisTp = Tp.WM;
            _gameType = (int)GameType.WM_LiveCasino;
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<WMConfig>(_thisTp.ToString());
        }

        #region Request

        public class Req
        {
            public string cmd { get; set; }
            public string signature { get; set; }
            public DateTime requestDate { get; set; }
            public string user { get; set; }
        }

        #endregion

        #region Response

        public class GetSeamlessWalletResp
        {
            public int errorCode { get; set; }
            public string errorMessage { get; set; }
        }

        public class CallBalanceResp : GetSeamlessWalletResp
        {
            public BalanceInfo result { get; set; }
        }

        public class BalanceInfo
        {
            public string user { get; set; }
            public decimal money { get; set; }
            public string responseDate { get; set; }
        }

        public class PointInoutResp : GetSeamlessWalletResp
        {
            public PointInoutInfo result { get; set; }
        }

        public class PointInoutInfo
        {
            public decimal money { get; set; }
            public string responseDate { get; set; }
            public string dealid { get; set; }
            public decimal cash { get; set; }
        }

        #endregion

        [Route("[controller]")]
        [HttpPost]
        public GetSeamlessWalletResp SeamlessWallet([FromForm] Dictionary<string, string> req)
        {
            var Content = JsonConvert.SerializeObject(req);
            Req getReq = JsonConvert.DeserializeObject<Req>(Content);
            string logName = $"{_thisTp}_{getReq.cmd}";

            GetSeamlessWalletResp resp = new GetSeamlessWalletResp();
            resp.errorCode = 1;

            if (getReq.signature == null)
            {
                resp.errorCode = 10304;
                resp.errorMessage = "signature empty";
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{_thisTp}_signature");
                return resp;
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getReq.user);
            if (tpCustInfo.SiteId == 0)
            {
                resp.errorCode = 10303;
                resp.errorMessage = "signature error (SiteId)";
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{_thisTp}_signature");
                return resp;
            }

            var config = _siteConfigDic[tpCustInfo.SiteId];
            string currency = tpCustInfo.TpCurrencyId;
            if (getReq.signature != config.CurrencyData[currency].signature)
            {
                resp.errorCode = 10303;
                resp.errorMessage = "signature error";
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{_thisTp}_signature");
                return resp;
            }

            if (string.Equals(getReq.cmd, "CallBalance", StringComparison.OrdinalIgnoreCase))
            {
                CallBalanceResp callBalanceResp = new CallBalanceResp
                {
                    errorCode = 1,
                    errorMessage = "fail",
                    result = new BalanceInfo
                    {
                        user = getReq.user,
                        responseDate = GetCurrentGameTime()
                    }
                };

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getReq.user);
                if (tpCheck == null)
                {
                    callBalanceResp.errorCode = 10501;
                    callBalanceResp.errorMessage = "Invalid user Id";
                    _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(callBalanceResp)}", logName);
                    return callBalanceResp;
                }

                if (tpCheck.Locked)
                {
                    callBalanceResp.errorCode = 10505;
                    callBalanceResp.errorMessage = "Account is Lock";
                    _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(callBalanceResp)}", logName);
                    return callBalanceResp;
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.user, _gameType);
                if (tpCustBalance != null)
                {
                    callBalanceResp.errorCode = 0;
                    callBalanceResp.errorMessage = "";
                    callBalanceResp.result.money = tpCustBalance.Balance;
                    _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(callBalanceResp)}", logName);
                    return callBalanceResp;
                }

                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(callBalanceResp)}", logName);
                return callBalanceResp;
            }
            else if (string.Equals(getReq.cmd, "PointInout", StringComparison.OrdinalIgnoreCase))
            {
                BettingDataLC_WM getPointInoutReq = JsonConvert.DeserializeObject<BettingDataLC_WM>(Content);
                PointInoutResp pointInoutResp = new PointInoutResp
                {
                    errorCode = 1,
                    errorMessage = "fail",
                    result = new PointInoutInfo
                    {
                        money = getPointInoutReq.money,
                        dealid = getPointInoutReq.dealid,
                        responseDate = GetCurrentGameTime(),
                    }
                };

                var result = 0;
                switch (getPointInoutReq.code)
                {
                    case "1":
                        // 加點
                        if (_environment.IsDevelopment() && getPointInoutReq.money >= 30m)
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            return pointInoutResp;
                        }
                        try
                        {
                            result = _settledBLL.PayoutLC_WM(getPointInoutReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                //var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getPointInoutReq.user, gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getPointInoutReq.user, _gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    case "2":
                        // 扣點
                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getPointInoutReq.user);
                        if (tpCheck == null)
                        {
                            pointInoutResp.errorCode = 10501;
                            pointInoutResp.errorMessage = "Invalid user Id";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            return pointInoutResp;
                        }
                        if (tpCheck.Locked)
                        {
                            pointInoutResp.errorCode = 10505;
                            pointInoutResp.errorMessage = "Account is Lock";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            return pointInoutResp;
                        }
                        if (_environment.IsDevelopment() && getPointInoutReq.money == -40m)
                        {
                            System.Threading.Thread.Sleep(60000);
                            //log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            //return pointInoutResp;
                        }
                        try
                        {
                            result = _bettingBLL.PlaceBetLC_WM(getPointInoutReq, out decimal tpAffterAmount);

                            if (result > 0)
                            {
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getPointInoutReq.user);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (tpAffterAmount < 0)
                            {
                                pointInoutResp.errorCode = 10805;
                                pointInoutResp.errorMessage = "Not Enough Balance";
                                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    case "3":
                    case "4":
                    case "5":
                        try
                        {
                            //  3:重對加點 4:重對扣點 5:重新派彩
                            result = _settledBLL.ResettleLC_WM(getPointInoutReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getPointInoutReq.user);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    default:
                        break;
                }

                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                return pointInoutResp;
            }
            else if (string.Equals(getReq.cmd, "TimeoutBetReturn", StringComparison.OrdinalIgnoreCase))
            {
                BettingDataLC_WM getTimeoutBetReturnReq = JsonConvert.DeserializeObject<BettingDataLC_WM>(Content);
                PointInoutResp pointInoutResp = new PointInoutResp
                {
                    errorCode = 1,
                    errorMessage = "fail",
                    result = new PointInoutInfo
                    {
                        money = getTimeoutBetReturnReq.money,
                        dealid = getTimeoutBetReturnReq.dealid,
                        responseDate = GetCurrentGameTime(),
                    }
                };
                int result = 0;
                switch (getTimeoutBetReturnReq.code)
                {
                    case "1":
                        if (_environment.IsDevelopment() && getTimeoutBetReturnReq.money >= 100m)
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            return pointInoutResp;
                        }
                        try
                        {
                            // 加點
                            result = _settledBLL.PayoutLC_WM(getTimeoutBetReturnReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getTimeoutBetReturnReq.user, _gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    case "2":
                        try
                        {
                            // 扣點
                            result = _settledBLL.CancelBetLC_WM(getTimeoutBetReturnReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                //var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getTimeoutBetReturnReq.user, gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getTimeoutBetReturnReq.user, _gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    default:
                        break;
                }

                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                return pointInoutResp;
            }

            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }

        private static string GetCurrentGameTime()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
