using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class AmigoBLL
    {
        private readonly Tools _tools;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;

        public AmigoBLL(IServiceProvider provider)
        {
            _thisTp = Tp.Amigo;
            _gameType = (int)GameType.Amigo_RNG;
            _tools = provider.GetRequiredService<Tools>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        #region Response

        public class CommonResp
        {
            public int error { get; set; }
            public string description { get; set; }
        }

        public class AuthResp : CommonResp
        {
            private decimal _cash = 0m;
            private decimal _bonus = 0m;
            public string userId { get; set; }
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    _ = decimal.TryParse(_cash.ToString("N3"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus
            {
                get
                {
                    _ = decimal.TryParse(_bonus.ToString("N3"), out _bonus);
                    return _bonus;
                }
                set
                {
                    _bonus = value;
                }
            }
        }

        public class BalanceResp : CommonResp
        {
            private decimal _cash = 0m;
            private decimal _bonus = 0m;
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    _ = decimal.TryParse(_cash.ToString("N3"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus
            {
                get
                {
                    _ = decimal.TryParse(_bonus.ToString("N3"), out _bonus);
                    return _bonus;
                }
                set
                {
                    _bonus = value;
                }
            }
        }

        public class BetResp : CommonResp
        {
            private decimal _cash = 0m;
            private decimal _bonus = 0m;
            public long transactionId { get; set; }
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    _ = decimal.TryParse(_cash.ToString("N3"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus
            {
                get
                {
                    _ = decimal.TryParse(_bonus.ToString("N3"), out _bonus);
                    return _bonus;
                }
                set
                {
                    _bonus = value;
                }
            }
            public int usedPromo { get; set; } = 0;
        }

        public class ResultResp : CommonResp
        {
            private decimal _cash = 0m;
            private decimal _bonus = 0m;
            public long transactionId { get; set; }
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    _ = decimal.TryParse(_cash.ToString("N3"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus
            {
                get
                {
                    _ = decimal.TryParse(_bonus.ToString("N3"), out _bonus);
                    return _bonus;
                }
                set
                {
                    _bonus = value;
                }
            }
        }

        public class BonusWinResp : ResultResp
        {
        }

        public class JackpotWinResp : ResultResp
        {
        }

        public class RefundResp : CommonResp
        {
            public long transactionId { get; set; }
        }

        #endregion

        #region Api Verify & Process

        public dynamic Authenticate(AuthReq getReq)
        {
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = ErrorCodeOfAmigo.InternalError.GetEnumDescription()
            };

            if (string.IsNullOrEmpty(getReq.token))
            {
                getErrorResponse.error = (int)ErrorCodeOfAmigo.PlayerInvalid;
                getErrorResponse.description = ErrorCodeOfAmigo.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }

            if (!_authBLL.AuthenticateByTpToken(getReq.token, out TpCustInfoAuth tpCustInfoAuth))
            {
                getErrorResponse.error = (int)ErrorCodeOfAmigo.PlayerInvalid;
                getErrorResponse.description = ErrorCodeOfAmigo.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
            if (tpCheck == null)
            {
                getErrorResponse.error = (int)ErrorCodeOfAmigo.PlayerInvalid;
                getErrorResponse.description = ErrorCodeOfAmigo.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }
            if (tpCheck.Locked)
            {
                getErrorResponse.error = (int)ErrorCodeOfAmigo.PlayerFrozen;
                getErrorResponse.description = ErrorCodeOfAmigo.PlayerFrozen.GetEnumDescription();
                return getErrorResponse;
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType);
            AuthResp response = new()
            {
                userId = tpCustInfoAuth.TpUserName,
                currency = tpCustInfoAuth.TpCurrencyId,
                cash = _tools.Floor(tpCustBalance.Balance, 3),
                error = (int)ErrorCodeOfAmigo.Success,
                description = ErrorCodeOfAmigo.Success.GetEnumDescription(),
            };
            return response;
        }

        public dynamic GetBalance(BalanceReq getReq)
        {
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = ErrorCodeOfAmigo.InternalError.GetEnumDescription()
            };

            // check player account exist
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getReq.userId);
            if (tpCustInfo == null)
            {
                getErrorResponse.error = (int)ErrorCodeOfAmigo.PlayerInvalid;
                getErrorResponse.description = ErrorCodeOfAmigo.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.userId, _gameType);
            BalanceResp response = new()
            {
                currency = tpCustBalance.TpCurrencyId,
                cash = _tools.Floor(tpCustBalance.Balance, 3),
                error = (int)ErrorCodeOfAmigo.Success,
                description = ErrorCodeOfAmigo.Success.GetEnumDescription(),
            };
            return response;
        }

        public dynamic PlaceBet(BettingData_Amigo getReq)
        {
            string logName = $"{_thisTp}_PlaceBet";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = ErrorCodeOfAmigo.InternalError.GetEnumDescription()
            };

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getReq.userId);
            if (tpCheck == null)
            {
                getErrorResponse.error = (int)ErrorCodeOfAmigo.PlayerInvalid;
                getErrorResponse.description = ErrorCodeOfAmigo.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }
            else if (tpCheck.Locked)
            {
                getErrorResponse.error = (int)ErrorCodeOfAmigo.PlayerFrozen;
                getErrorResponse.description = ErrorCodeOfAmigo.PlayerFrozen.GetEnumDescription();
                return getErrorResponse;
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.userId, _gameType);
            getReq.TicketType = (int)BettransType.Bet;
            int result = PlaceBetRNG_Amigo(getReq, out decimal tpAffterAmount);
            if (result > 0 || result == -1)
            {
                if (tpAffterAmount == 0 || result == -1)
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.userId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }
                BetResp response = new()
                {
                    transactionId = getReq.roundId,
                    currency = tpCustBalance.TpCurrencyId,
                    cash = _tools.Floor(tpAffterAmount, 3),
                    error = (int)ErrorCodeOfAmigo.Success,
                    description = ErrorCodeOfAmigo.Success.GetEnumDescription(),
                };
                return response;
            }
            else if (tpAffterAmount < 0)
            {
                getErrorResponse.error = (int)ErrorCodeOfAmigo.Insufficient;
                getErrorResponse.description = ErrorCodeOfAmigo.Insufficient.GetEnumDescription();
                _log2File.Error($"result={result}, tpAffterAmount={tpAffterAmount}", logName);
                return getErrorResponse;
            }

            _log2File.Error($"result={result}", logName);
            return getErrorResponse;
        }

        public dynamic Payout(PayoutData_Amigo getReq)
        {
            string logName = $"{_thisTp}_Payout";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = ErrorCodeOfAmigo.InternalError.GetEnumDescription()
            };

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.userId, _gameType);
            getReq.TicketType = (int)BettransType.Payout;
            int result = PayoutRNG_Amigo(getReq, out decimal tpAffterAmount);
            if (result > 0 || result == -1)
            {
                if (tpAffterAmount == 0 || result == -1)
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.userId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                ResultResp response = new()
                {
                    transactionId = getReq.roundId,
                    currency = tpCustBalance.TpCurrencyId,
                    cash = _tools.Floor(tpAffterAmount, 3),
                    error = (int)ErrorCodeOfAmigo.Success,
                    description = ErrorCodeOfAmigo.Success.GetEnumDescription(),
                };
                return response;
            }
            _log2File.Error($"result={result}", logName);
            return getErrorResponse;
        }

        public dynamic BonusWin(PayoutData_Amigo getReq)
        {
            string logName = $"{_thisTp}_BonusWin";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = ErrorCodeOfAmigo.InternalError.GetEnumDescription()
            };

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.userId, _gameType);
            getReq.TicketType = (int)BettransType.Bonus;
            int result = PayoutRNG_Amigo(getReq, out decimal tpAffterAmount);
            if (result > 0 || result == -1)
            {
                if (tpAffterAmount == 0 || result == -1)
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.userId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                BonusWinResp response = new()
                {
                    transactionId = getReq.roundId,
                    currency = tpCustBalance.TpCurrencyId,
                    cash = _tools.Floor(tpAffterAmount, 3),
                    error = (int)ErrorCodeOfAmigo.Success,
                    description = ErrorCodeOfAmigo.Success.GetEnumDescription(),
                };
                return response;
            }
            _log2File.Error($"result={result}", logName);
            return getErrorResponse;
        }

        public dynamic JackpotWin(PayoutData_Amigo getReq)
        {
            string logName = $"{_thisTp}_JackpotWin";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = ErrorCodeOfAmigo.InternalError.GetEnumDescription()
            };

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.userId, _gameType);
            getReq.TicketType = (int)BettransType.Jackpot;
            int result = PayoutRNG_Amigo(getReq, out decimal tpAffterAmount);
            if (result > 0 || result == -1)
            {
                if (tpAffterAmount == 0 || result == -1)
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.userId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                JackpotWinResp response = new()
                {
                    transactionId = getReq.roundId,
                    currency = tpCustBalance.TpCurrencyId,
                    cash = _tools.Floor(tpAffterAmount, 3),
                    error = (int)ErrorCodeOfAmigo.Success,
                    description = ErrorCodeOfAmigo.Success.GetEnumDescription(),
                };
                return response;
            }
            _log2File.Error($"result={result}", logName);
            return getErrorResponse;
        }

        public dynamic CancelBet(BettingData_Amigo getReq)
        {
            string logName = $"{_thisTp}_CancelBet";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = ErrorCodeOfAmigo.InternalError.GetEnumDescription()
            };

            getReq.TicketType = (int)BettransType.CancelBet;
            int result = CancelBetRNG_Amigo(getReq);
            if (result > 0 || result == -1)
            {
                RefundResp response = new()
                {
                    transactionId = getReq.roundId,
                    error = (int)ErrorCodeOfAmigo.Success,
                    description = ErrorCodeOfAmigo.Success.GetEnumDescription(),
                };
                return response;
            }
            _log2File.Error($"result={result}", logName);
            return getErrorResponse;
        }

        #endregion

        #region ExchangeRate & Service

        private int PlaceBetRNG_Amigo(BettingData_Amigo bettingData_Amigo, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingData_Amigo.userId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingData_Amigo.amount *= tpCurrency.ExchangeRate;
            }

            var result = _bettingService.PlaceBetRNG_Amigo(tpCustInfo, bettingData_Amigo, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int PayoutRNG_Amigo(PayoutData_Amigo payoutData_Amigo, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, payoutData_Amigo.userId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                payoutData_Amigo.amount *= tpCurrency.ExchangeRate;
            }

            var result = _settledService.PayoutRNG_Amigo(tpCustInfo, payoutData_Amigo, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int CancelBetRNG_Amigo(BettingData_Amigo bettingData_Amigo)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingData_Amigo.userId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingData_Amigo.amount *= tpCurrency.ExchangeRate;
            }

            return _settledService.CancelBetRNG_Amigo(tpCustInfo, bettingData_Amigo);
        }

        #endregion
    }
}
