using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class EdayBLL : IEdayBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;

        public EdayBLL(IServiceProvider provider)
        {
            _thisTp = Tp.Eday;
            _gameType = (int)GameType.Eday_Lottery;
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public EdayResponse<EdayGetBalanceResponse> GetBalance(string accountName)
        {
            var getErrorResponse = new EdayResponse<EdayGetBalanceResponse>()
            {
                resultCode = (int)EdayResultCode.ServerError,
                message = EdayResultCode.ServerError.GetEnumDescription(),
                data = null
            };
            
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, accountName);
            
            if (tpCheck == null)
            {
                getErrorResponse.resultCode  = (int)EdayResultCode.PlayerAccountNotExist;
                getErrorResponse.message = EdayResultCode.PlayerAccountNotExist.GetEnumDescription();
                return getErrorResponse;
            }
            
            if (tpCheck.Locked)
            {
                getErrorResponse.resultCode  = (int)EdayResultCode.PlayerFrozen;
                getErrorResponse.message = EdayResultCode.PlayerFrozen.GetEnumDescription();
                return getErrorResponse;
            }
            
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, accountName, _gameType);
            var response = new EdayResponse<EdayGetBalanceResponse>()
            {
                resultCode = (int)EdayResultCode.Success,
                message = EdayResultCode.Success.GetEnumDescription(),
                data = new EdayGetBalanceResponse()
                {
                    balance = tpCustBalance.Balance
                }
            };
            
            return response;
        }

        public EdayResponse<EdayBetRequestResponse> BetRequest(EdayBetRequest request)
        {
            var logName = $"{_thisTp.ToString()}_BetRequest";
            var getErrorResponse = new EdayResponse<EdayBetRequestResponse>()
            {
                resultCode = (int)EdayResultCode.ServerError,
                message = EdayResultCode.ServerError.GetEnumDescription(),
                data = null
            };

            try
            {
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.Eday, request.player);
                
                if (tpCheck == null)
                {
                    getErrorResponse.resultCode  = (int)EdayResultCode.PlayerAccountNotExist;
                    getErrorResponse.message = EdayResultCode.PlayerAccountNotExist.GetEnumDescription();
                    return getErrorResponse;
                }
            
                if (tpCheck.Locked)
                {
                    getErrorResponse.resultCode  = (int)EdayResultCode.PlayerFrozen;
                    getErrorResponse.message = EdayResultCode.PlayerFrozen.GetEnumDescription();
                    return getErrorResponse;
                }
                
                request.TicketType = (int)BettransType.Bet;
                request.TicketStatus = (int)TicketStatus.UnSettled;
                request.UTCBetTime = DateTimeOffset.FromUnixTimeMilliseconds(request.timestamp).DateTime;
                
                var result = BetRequest_Eday(request, out decimal tpAffterAmount);
                
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.player, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    
                    return new EdayResponse<EdayBetRequestResponse>()
                    {
                        resultCode = (int)EdayResultCode.Success,
                        message = EdayResultCode.Success.GetEnumDescription(),
                        data = new EdayBetRequestResponse()
                        {
                            balance = tpAffterAmount
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                getErrorResponse.resultCode = (int)EdayResultCode.ServerError;
                getErrorResponse.message = EdayResultCode.ServerError.GetEnumDescription();
                _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }
            
            _log2File.Error($"result={getErrorResponse}", logName);
            return getErrorResponse;
        }

        public EdayResponse<EdayBetRequestResponse> BetRequestCancel(EdayBetRequest request)
        {
            throw new NotImplementedException();
        }

        #region ExChange
        
        private int BetRequest_Eday(EdayBetRequest bettingDataLT_Eday, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingDataLT_Eday.player);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
            var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, bettingDataLT_Eday.player, _gameType);

            //目前只有開放THB，但查看文件有支援多種幣別，所以一樣有匯率換算。
            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataLT_Eday.amount *= tpCurrency.ExchangeRate;
                custBalance.Balance *= tpCurrency.ExchangeRate;
            }

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                return 0;
            }

            var checkBalance = custBalance.Balance - Math.Abs(bettingDataLT_Eday.amount);
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                return 0;
            }
            
            var result = _bettingService.BetRequest_Eday(tpCustInfo, bettingDataLT_Eday, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        
        #endregion
    }
}