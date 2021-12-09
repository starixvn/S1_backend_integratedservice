using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL
{
    public class TpTransferInfo
    {
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly WalletService walletService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public TpTransferInfo(IServiceProvider provider, IConfiguration configuration, Common common, WalletService walletService)
        {
            this.configuration = configuration;
            this.common = common;
            this.walletService = walletService;
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public ApiResult<TransferResult> TpTransferReqV2(TpTransfer tpTransfer, TpCustInfo tpCustInfo, string thirdId, string prefix, object extensionInfo, out string transId)
        {
            var apiResult = new ApiResult<TransferResult>();
            var transferId = 0;
            var prefixName = $"{prefix}{common.GetEnv()}";
            using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                transId = conn.QueryFirstOrDefault<string>("Service_TpTransferInfo_Ins",
                    new
                    {
                        tpTransfer.TpId,
                        tpCustInfo.CustId,
                        tpCustInfo.UserName,
                        tpCustInfo.SiteId,
                        Direction = (int)tpTransfer.direction,
                        tpTransfer.Amount,
                        Memo = (string)null,
                        ThirdId = thirdId,
                        Prefix = prefixName,
                        ExtensionInfo = extensionInfo != null ? JsonConvert.SerializeObject(extensionInfo) : null,
                        BonusId = tpTransfer.BonusId
                    }, commandType: CommandType.StoredProcedure);
                conn.Open();

                var m = Regex.Match(transId, @"\d+");
                int.TryParse(m.Value, out transferId);

                if (tpTransfer.direction == Direction.ToTp)
                {
                    using (var trans = conn.BeginTransaction())
                    {
                        decimal afterAmount = -1;
                        var rwalletServiceResult = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount * -1, tpTransfer.TpId, null, transId, out afterAmount, conn, trans);

                        if (rwalletServiceResult != 0)
                        {
                            apiResult.ErrorCode = ErrorCode.MoneyNotEnough;
                            apiResult.Data.Difference = afterAmount;
                            apiResult.Message = "Amount isn't enough";

                            conn.Execute("TpTransferInfo_Set", new { TransferId = transferId, ThirdId = thirdId, Memo = $"amount isn't not enough: {Math.Abs(afterAmount)}", ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.Fail }, trans, commandType: CommandType.StoredProcedure);
                            trans.Commit();
                            return apiResult;
                        }
                        else
                        {
                            conn.Execute("TpTransferInfo_Set", new { TransferId = transferId, ThirdId = thirdId, Memo = (string)null, ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.PendingD }, trans, commandType: CommandType.StoredProcedure);
                            trans.Commit();
                        }
                    }
                }
            }

            apiResult.ErrorCode = ErrorCode.Success;
            return apiResult;
        }

        public bool Service_TpTransferInfo_Bonus_Get(int bonusId)
        {
            return _reportDbClient.QueryFirst<bool>("Service_TpTransferInfo_Bonus_Get",
                new DynamicParameters(new { BonusId = bonusId }));
        }

        public void TpTransferCommit(ApiResult<TransferResult> transferResult, TpCustInfo tpCustInfo, TpTransfer tpTransfer, string transId)
        {
            decimal afterAmount = 0;
            var transferId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out transferId);

            if (transferResult.Data.Status == TransferStatus.Success && transferResult.ErrorCode == ErrorCode.Success)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                var forStatus = 0;
                forStatus = tpTransfer.direction == Direction.ToTp ? (int)TransferStatus.PendingD : (int)TransferStatus.Pending;

                var isSuccess = conn.Execute("TpTransferInfo_Set",
                    new
                    {
                        TransferId = transferId, 
                        transferResult.Data.ThirdId, 
                        Memo = transferResult.RawMessage,
                        ForStatus = forStatus, 
                        Status = (int)TransferStatus.Success
                    }, trans, commandType: CommandType.StoredProcedure) > 0;
                var rwalletServiceResult = 0;
                if (tpTransfer.direction == Direction.ToMain && isSuccess)
                {
                    rwalletServiceResult = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount, tpTransfer.TpId, null, transId, out afterAmount, conn, trans);
                }
                else if (tpTransfer.direction == Direction.ToTp)
                {
                    var custBonus = conn.QueryFirstOrDefault<BonusWallet>("Service_CustBonus_Check", new { tpCustInfo.CustId, tpTransfer.TpId }, trans, commandType: CommandType.StoredProcedure);
                    if (custBonus != null)
                    {
                        var bonusId = custBonus.BonusId;
                        string memo = null;
                        var withdrawalLimit = tpTransfer.Amount * -1;
                        var p = new DynamicParameters(new
                        {
                            tpCustInfo.CustId,
                            tpCustInfo.UserName,
                            tpCustInfo.SiteId,
                            Memo = memo,
                            RefTransId = transId
                        });

                        p.Add("AfterWithdrawalLimit", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                        p.Add("WithdrawalLimit", withdrawalLimit, dbType: DbType.Decimal, direction: ParameterDirection.InputOutput);
                        var res = conn.Execute("Service_WithdrawalLimit_Set", p, trans, commandType: CommandType.StoredProcedure);
                        // if (res > 0)
                        // {
                        //     var rollover = p.Get<decimal>("WithdrawalLimit");
                        //     conn.Execute("Service_BonusAmount_Set", new { BonusId = bonusId, LockedAmount = tpTransfer.Amount, Rollover = Math.Abs(rollover) }, trans, commandType: CommandType.StoredProcedure);
                        // }
                    }
                }

                if (rwalletServiceResult == 0)
                    trans.Commit();
            }
            else if (transferResult.Data.Status == TransferStatus.Fail)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                var forStatus = 0;
                forStatus = tpTransfer.direction == Direction.ToTp ? (int)TransferStatus.PendingD : (int)TransferStatus.Pending;
                string Memo = null;
                if (transferResult.ErrorCode == ErrorCode.MoneyNotEnough)
                {
                    Memo = "3RD wallet amount isn't enough";
                }
                else if (!string.IsNullOrEmpty(transferResult.RawMessage))
                {
                    Memo = transferResult.RawMessage;
                }

                var p = new DynamicParameters(new { TransferId = transferId, transferResult.Data.ThirdId, ForStatus = forStatus, Status = (int)TransferStatus.Fail });
                p.Add("Memo", Memo, dbType: DbType.String, direction: ParameterDirection.Input, size: 100);
                var isSuccess = conn.Execute("TpTransferInfo_Set", p, trans, commandType: CommandType.StoredProcedure) > 0;
                var rwalletServiceResult = 0;
                if (tpTransfer.direction == Direction.ToTp && isSuccess)
                {
                    Memo = $"\r\nreturn amount. Cause: {transferResult.RawMessage}";
                    rwalletServiceResult = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount, tpTransfer.TpId, Memo, transId, out afterAmount, conn, trans);
                }

                trans.Commit();
            }
        }

        public IEnumerable<TpTransferInfoModel> TpTransferPendingGet()
        {
            using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<TpTransferInfoModel>("TpTransferInfo_Pending_Get", commandType: CommandType.StoredProcedure);
            }
        }
        public IEnumerable<TpPendingBonusTransferModel> TpBonusTransferPendingGet()
        {
            return _mainDbClient.Query<TpPendingBonusTransferModel>("Admin_PendingBonusList_Get");
        }
        public TpPendingBonusTransferInfoModel TpBonusTransferInfoPendingGet(int custId , string status)
        {
            return _mainDbClient.QueryFirst<TpPendingBonusTransferInfoModel>("Service_TpTransferBonusInfoPendingByCustId_Get_V2",new DynamicParameters(new {_CustId = custId , _Status = status}));
        }
        public bool SetTransferFail(string transId)
        {
            var transferId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out transferId);

            using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Execute("TpTransferInfo_Set", new { TransferId = transferId, ThirdId = (string)null, Memo = (string)null, ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.Fail }, commandType: CommandType.StoredProcedure) > 0;
            }
        }

        public void InsAdjustWalletLog(TpCustInfo tpCustInfo, AdminTpTransfer adminTpTransfer, out string transId)
        {
            using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                var p = new DynamicParameters(new { Prefix = $"T{common.GetEnv()}", adminTpTransfer.TpId, tpCustInfo.SiteId, tpCustInfo.CustId, tpCustInfo.UserName, Direction = adminTpTransfer.direction, adminTpTransfer.Amount, adminTpTransfer.Operator, adminTpTransfer.Remarks });
                transId = conn.QueryFirstOrDefault<string>("Log_AdminAdjustWallet_Ins", p, commandType: CommandType.StoredProcedure);
            }
        }
        public void SetAdjustWalletLog(ApiResult<TransferResult> transferResult, string transId)
        {
            using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                string Memo = null;
                if (transferResult.ErrorCode == ErrorCode.MoneyNotEnough)
                {
                    Memo = "3RD wallet amount isn't enough";
                }
                else if (!string.IsNullOrEmpty(transferResult.RawMessage))
                {
                    Memo = transferResult.RawMessage;
                }

                var p = new DynamicParameters(new { TransId = transId, transferResult.Data.ThirdId, Memo, transferResult.Data.Status });
                conn.QueryFirstOrDefault<string>("Log_AdminAdjustWallet_Set", p, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
