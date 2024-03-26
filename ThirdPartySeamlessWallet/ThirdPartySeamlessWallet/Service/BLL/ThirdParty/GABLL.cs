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

        public GABLL(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
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

            //檢查是否重複下注
            if (check.Any())
            {
                return (int)GATransactionStatus.DuplicateBet;
            }

            if (reqData.type == (int)GAType.Uncancel)
            {
                var cancelCheck = _partitionService.Query<BettingData_GA>(partitionTime, "Service_Bettrans_GA_CheckBySid", new { sid = reqData.ticket.sid }).ToList().Find(x => x.type == (int)GAType.TicketCancelation);

                //檢查是否有這筆注單
                if (cancelCheck == null)
                {
                    return (int)GATransactionStatus.NoCancel;
                }
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
                fight_number = reqData.ticket.fight_number,
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
                TicketType = (int)BettransType.Bet
            };

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.GA, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.bet_amount *= tpCurrency.ExchangeRate;
                bettingData.ticket_amount *= tpCurrency.ExchangeRate;
                bettingData.ante_amount *= tpCurrency.ExchangeRate;
                bettingData.win_loss_amount *= tpCurrency.ExchangeRate;
                bettingData.amount *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_Bettrans_GA_Ins", bettingData, trans);

            if (tempResult > 0)
            {
                if (reqData.type == (int)GAType.Uncancel)
                {
                    tempResult = _settledService.PayoutV2(
                            tpCustInfo,
                            Math.Abs(bettingData.amount) * -1,
                            (int)Tp.GA,
                            bettingData.sid,
                            (int)GameType.GA_CockFighting,
                            TpCategory.Cockfighting,
                            bettingData.sid,
                            TransType.Wagers,
                            null,
                            out afterAmount,
                            ((GAType)reqData.type).ToString(),
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow);
                }
                else
                {
                    tempResult = _bettingService.PlaceBet(tpCustInfo, Math.Abs(bettingData.amount) * -1, (int)Tp.GA, bettingData.sid, (int)GameType.GA_CockFighting, bettingData.sid, out afterAmount, ref bonusId, out _, conn, trans, string.Empty);
                }
            }

            if (tempResult > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return tempResult > 0 ? (int)GATransactionStatus.Success : (int)GATransactionStatus.InsufficientBalance;
        }

        public int Payout(TpCustInfo tpCustInfo, GATransferReq reqData)
        {
            var tempResult = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            decimal afterAmount = 0;
            var partitionTime = reqData.ticket.created_date_time;

            var bettransData = _partitionService.Query<BettingData_GA_Detail>(partitionTime, "Service_Bettrans_GA_CheckBySid", new { sid = reqData.ticket.sid }).ToList();
            var betCheck = bettransData.Find(x => x.type == (int)GAType.Bet);
            var previousTran = bettransData.OrderByDescending(x => x.CreateTime).FirstOrDefault();

            //檢查是否有這筆注單
            if (betCheck == null)
            {
                return (int)GATransactionStatus.NoBet;
            }

            var check = _partitionService.Query<BettingData_GA>(partitionTime, "Service_Bettrans_GA_Check", new { id = reqData.id });

            //檢查是否重複payout
            if (check.Any())
            {
                return (int)GATransactionStatus.DuplicatePay;
            }

            //檢查目前住單的狀態是不是cancel(如果是的話要先uncancel才能payout)
            if (previousTran.type == (int)GAType.TicketCancelation)
            {
                return (int)GATransactionStatus.BetIsCancel;
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
                fight_number = reqData.ticket.fight_number,
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
                TicketType = (int)BettransType.Payout
            };

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.GA, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.bet_amount *= tpCurrency.ExchangeRate;
                bettingData.ticket_amount *= tpCurrency.ExchangeRate;
                bettingData.ante_amount *= tpCurrency.ExchangeRate;
                bettingData.win_loss_amount *= tpCurrency.ExchangeRate;
                bettingData.amount *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_Bettrans_GA_Ins", bettingData, trans);

            if (tempResult > 0 & bettingData.amount > 0)
            {
                tempResult = _settledService.PayoutV2(
                        tpCustInfo,
                        bettingData.amount,
                        (int)Tp.GA,
                        bettingData.sid,
                        (int)GameType.GA_CockFighting,
                        TpCategory.Cockfighting,
                        bettingData.sid,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        ((GAType)reqData.type).ToString(),
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);

            }

            if (tempResult > 0)
            {
                _ = _tpUnsettleService.InsertTpUnsettle(Tp.GA, GameType.GA_CockFighting, bettingData.sid, trans);
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);

                return (int)GATransactionStatus.Fail;
            }

            return (int)GATransactionStatus.Success;
        }


        public int RollBack(TpCustInfo tpCustInfo, GATransferReq reqData)
        {
            var tempResult = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            decimal afterAmount = 0;
            var partitionTime = reqData.ticket.created_date_time;

            var betCheck = _partitionService.Query<BettingData_GA>(partitionTime, "Service_Bettrans_GA_CheckBySid", new { sid = reqData.ticket.sid }).ToList().Find(x => x.type == (int)GAType.Bet);

            //檢查是否有這筆注單
            if (betCheck == null)
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
                fight_number = reqData.ticket.fight_number,
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
                TicketType = (int)BettransType.CancelBet
            };

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.GA, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.bet_amount *= tpCurrency.ExchangeRate;
                bettingData.ticket_amount *= tpCurrency.ExchangeRate;
                bettingData.ante_amount *= tpCurrency.ExchangeRate;
                bettingData.win_loss_amount *= tpCurrency.ExchangeRate;
                bettingData.amount *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_Bettrans_GA_Ins", bettingData, trans);

            if (tempResult > 0)
            {
                string memo = reqData.type == (int)GAType.TicketWinnings ? "Winnings Payout" : "Draw Payout";

                tempResult = _settledService.PayoutV2(
                        tpCustInfo,
                        bettingData.amount,
                        (int)Tp.GA,
                        bettingData.sid,
                        (int)GameType.GA_CockFighting,
                        TpCategory.Cockfighting,
                        bettingData.sid,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        ((GAType)reqData.type).ToString(),
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);

            }

            if (tempResult > 0)
            {
                if (reqData.type == (int)GAType.Void)
                {
                    _ = _tpUnsettleService.InsertTpUnsettle(Tp.GA, GameType.GA_CockFighting, bettingData.sid, trans);
                }
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return tempResult > 0 ? (int)GATransactionStatus.Success : (int)GATransactionStatus.Fail;
        }

        public int Unsettle(TpCustInfo tpCustInfo, GATransferReq reqData)
        {
            var tempResult = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            decimal afterAmount = 0;
            var partitionTime = reqData.ticket.created_date_time;

            var check = _partitionService.Query<BettingData_GA>(partitionTime, "Service_Bettrans_GA_CheckBySid", new { sid = reqData.ticket.sid });

            //檢查是否有這筆注單
            if (!check.Any(x => x.TicketType == (int)BettransType.Payout))
            {
                return (int)GATransactionStatus.Fail;
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
                fight_number = reqData.ticket.fight_number,
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
                TicketType = (int)BettransType.Reserve
            };

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.GA, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.bet_amount *= tpCurrency.ExchangeRate;
                bettingData.ticket_amount *= tpCurrency.ExchangeRate;
                bettingData.ante_amount *= tpCurrency.ExchangeRate;
                bettingData.win_loss_amount *= tpCurrency.ExchangeRate;
                bettingData.amount *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_Bettrans_GA_Ins", bettingData, trans);

            if (tempResult > 0)
            {
                tempResult = _settledService.PayoutV2(
                        tpCustInfo,
                        bettingData.amount,
                        (int)Tp.GA,
                        bettingData.sid,
                        (int)GameType.GA_CockFighting,
                        TpCategory.Cockfighting,
                        bettingData.sid,
                        TransType.Undo,
                        null,
                        out afterAmount,
                        ((GAType)reqData.type).ToString(),
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);
            }

            if (tempResult > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return tempResult > 0 ? (int)GATransactionStatus.Success : (int)GATransactionStatus.Fail;
        }

        public int Jackpot(TpCustInfo tpCustInfo, GATransferReq reqData)
        {
            var tempResult = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            decimal afterAmount = 0;
            var partitionTime = DateTime.Now;

            var check = _partitionService.Query<BettingData_GA>(partitionTime, "Service_Bettrans_GA_Check", new { id = reqData.id });

            //檢查是否重複
            if (check.Any())
            {
                return (int)GATransactionStatus.Fail;
            }

            var bettingData = new BettingData_GA()
            {
                id = reqData.id,
                member_ref_id = reqData.member_ref_id,
                type = reqData.type,
                amount = Convert.ToDecimal(reqData.amount),
                TicketType = (int)BettransType.Jackpot
            };

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.GA, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.amount *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_Bettrans_GA_Ins", bettingData, trans);

            if (tempResult > 0)
            {
                tempResult = _settledService.PayoutV2(
                        tpCustInfo,
                        bettingData.amount,
                        (int)Tp.GA,
                        bettingData.id,
                        (int)GameType.GA_CockFighting,
                        TpCategory.Cockfighting,
                        bettingData.id,
                        TransType.Prize,
                        null,
                        out afterAmount,
                        ((GAType)reqData.type).ToString(),
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);
            }

            if (tempResult > 0)
            {
                _ = _tpUnsettleService.InsertTpUnsettle(Tp.GA, GameType.GA_CockFighting, bettingData.id, trans);
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return tempResult > 0 ? (int)GATransactionStatus.Success : (int)GATransactionStatus.Fail;
        }
    }
}
