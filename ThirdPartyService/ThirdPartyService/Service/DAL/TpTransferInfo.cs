using Dapper;
using System.Data;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL
{
    public class TpTransferInfo
    {
        private readonly Common _common;
        private readonly WalletService _walletService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public TpTransferInfo(IServiceProvider provider)
        {
            _common = provider.GetRequiredService<Common>();
            _walletService = provider.GetRequiredService<WalletService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public ApiResult<TransferResult> TpTransferReqV2(TpTransfer tpTransfer, TpCustInfo tpCustInfo, string thirdId, string prefix, object extensionInfo, out string transId)
        {
            var apiResult = new ApiResult<TransferResult>();
            var prefixName = $"{prefix}{_common.GetEnv()}";
            
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            transId = _mainDbClient.QueryFirst<string>("Service_TpTransferInfo_Ins",
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
                });

            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var transferId);

            if (tpTransfer.direction == Direction.ToTp)
            {
                var rwalletServiceResult = _walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount * -1, tpTransfer.TpId, null, transId, out var afterAmount, trans);

                if (rwalletServiceResult != 0)
                {
                    apiResult.ErrorCode = ErrorCode.MoneyNotEnough;
                    apiResult.Data.Difference = afterAmount;
                    apiResult.Message = "Amount isn't enough";

                    _mainDbClient.Execute("TpTransferInfo_Set", trans ,new { TransferId = transferId, ThirdId = thirdId, Memo = $"amount isn't not enough: {Math.Abs(afterAmount)}", ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.Fail });
                    trans.Commit();
                    conn.Close();
                    conn.Dispose();
                    return apiResult;
                }
                else
                {
                    _mainDbClient.Execute("TpTransferInfo_Set", trans ,new { TransferId = transferId, ThirdId = thirdId, Memo = (string)null, ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.PendingD });
                    trans.Commit();
                }
            }
            
            conn.Close();
            conn.Dispose();
            apiResult.ErrorCode = ErrorCode.Success;
            return apiResult;
        }

        public bool Service_TpTransferInfo_Bonus_Get(int bonusId)
        {
            return _reportDbClient.QueryFirst<bool>("Service_TpTransferInfo_Bonus_Get", new DynamicParameters(new { BonusId = bonusId }));
        }

        public void TpTransferCommit(ApiResult<TransferResult> transferResult, TpCustInfo tpCustInfo, TpTransfer tpTransfer, string transId)
        {
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var transferId);

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            if (transferResult.Data.Status == TransferStatus.Success && transferResult.ErrorCode == ErrorCode.Success)
            {
                var forStatus = tpTransfer.direction == Direction.ToTp ? (int)TransferStatus.PendingD : (int)TransferStatus.Pending;

                var isSuccess = _mainDbClient.Execute("TpTransferInfo_Set",trans,
                    new
                    {
                        TransferId = transferId, 
                        transferResult.Data.ThirdId, 
                        Memo = transferResult.RawMessage,
                        ForStatus = forStatus, 
                        Status = (int)TransferStatus.Success
                    }) > 0;
                var rwalletServiceResult = 0;
                if (tpTransfer.direction == Direction.ToMain && isSuccess)
                {
                    rwalletServiceResult = _walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount, tpTransfer.TpId, null, transId, out _, trans);
                }
                else if (tpTransfer.direction == Direction.ToTp)
                {
                    var custBonus = _mainDbClient.QueryFirst<BonusWallet>("Service_CustBonus_Check", new { tpCustInfo.CustId, tpTransfer.TpId });
                    if (custBonus != null)
                    {
                        string memo = null;
                        var withdrawalLimit = tpTransfer.Amount * -1;
                        var p = new DynamicParameters(new
                        {
                            tpCustInfo.CustId,
                            tpCustInfo.UserName,
                            tpCustInfo.SiteId,
                            Memo = memo,
                            RefTransId = transId,
                            CreateFrom = (int)WithdrawalLimitLogCreateFromEnum.Bonus, 
                            Creator = "System"
                        });

                        p.Add("AfterWithdrawalLimit", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                        p.Add("WithdrawalLimit", withdrawalLimit, dbType: DbType.Decimal, direction: ParameterDirection.InputOutput);
                        var res = _mainDbClient.Execute("Service_WithdrawalLimit_Set_V2", trans, p);
                    }
                }

                if (rwalletServiceResult is 0 or -1)
                    trans.Commit();
            }
            else if (transferResult.Data.Status == TransferStatus.Fail)
            {
                var forStatus = tpTransfer.direction == Direction.ToTp ? (int)TransferStatus.PendingD : (int)TransferStatus.Pending;
                string memo = null;
                if (transferResult.ErrorCode == ErrorCode.MoneyNotEnough)
                {
                    memo = "3RD wallet amount isn't enough";
                }
                else if (!string.IsNullOrEmpty(transferResult.RawMessage))
                {
                    memo = transferResult.RawMessage;
                }

                var p = new DynamicParameters(new { TransferId = transferId, transferResult.Data.ThirdId, ForStatus = forStatus, Status = (int)TransferStatus.Fail });
                p.Add("Memo", memo, dbType: DbType.String, direction: ParameterDirection.Input, size: 100);
                var isSuccess = _mainDbClient.Execute("TpTransferInfo_Set", trans ,p) > 0;
                var rwalletServiceResult = 0;
                if (tpTransfer.direction == Direction.ToTp && isSuccess)
                {
                    memo = $"\r\nreturn amount. Cause: {transferResult.RawMessage}";
                    rwalletServiceResult = _walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount, tpTransfer.TpId, memo, transId, out _, trans);
                }

                if (rwalletServiceResult == 0)
                    trans.Commit();
            }
            
            conn.Close();
            conn.Dispose();
        }

        public IEnumerable<TpTransferInfoModel> TpTransferPendingGet(int limit)
        {
            return _mainDbClient.QueryUseCommandDefinition<TpTransferInfoModel>("TpTransferInfo_Pending_Get_V2",new DynamicParameters(new { _Limit = limit}));
        }
        public IEnumerable<TpPendingBonusTransferModel> TpBonusTransferPendingGet(int limit)
        {
            return _mainDbClient.QueryUseCommandDefinition<TpPendingBonusTransferModel>("Service_PendingBonusList_Get",new DynamicParameters(new { _Limit = limit}));
        }
        public TpPendingBonusTransferInfoModel TpBonusTransferInfoPendingGet(int custId , string status)
        {
            return _mainDbClient.QueryFirst<TpPendingBonusTransferInfoModel>("Service_TpTransferBonusInfoPendingByCustId_Get_V2",new DynamicParameters(new {_CustId = custId , _Status = status}));
        }
        public IEnumerable<TpRemainingTransferBonusModel> TpRemainingTransferBonusGet(int limit)
        {
            return _mainDbClient.QueryUseCommandDefinition<TpRemainingTransferBonusModel>("Service_RemainingTransferBonusPending_Get_V2",new DynamicParameters(new { _Limit = limit}));
        }
        
        public bool SetTransferFail(string transId)
        {
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var transferId);
            
            return _mainDbClient.Execute("TpTransferInfo_Set", new { TransferId = transferId, ThirdId = (string)null, Memo = (string)null, ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.Fail }) > 0;
        }

        public void InsAdjustWalletLog(TpCustInfo tpCustInfo, AdminTpTransfer adminTpTransfer, out string transId)
        {
            var p = new DynamicParameters(new
            {
                Prefix = $"T{_common.GetEnv()}",
                adminTpTransfer.TpId,
                tpCustInfo.SiteId,
                tpCustInfo.CustId,
                tpCustInfo.UserName,
                Direction = adminTpTransfer.direction,
                adminTpTransfer.Amount,
                adminTpTransfer.Operator,
                adminTpTransfer.Remarks,
                adminTpTransfer.Category
            });
            
            transId = _mainDbClient.QueryFirst<string>("Log_AdminAdjustWallet_Ins_V2", p);
        }
        
        public void SetAdjustWalletLog(ApiResult<TransferResult> transferResult, string transId)
        {
            string memo = null;
            if (transferResult.ErrorCode == ErrorCode.MoneyNotEnough)
            {
                memo = "3RD wallet amount isn't enough";
            }
            else if (!string.IsNullOrEmpty(transferResult.RawMessage))
            {
                memo = transferResult.RawMessage;
            }

            var p = new DynamicParameters(new { TransId = transId, transferResult.Data.ThirdId, Memo = memo, transferResult.Data.Status });
            _mainDbClient.QueryFirst<string>("Log_AdminAdjustWallet_Set", p);
        }
        
        public int SetAdjustmentActualDeposit(string transId, int depositId, decimal adjustAmount)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var props = new DynamicParameters();
            props.Add("_DepositId", depositId);
            props.Add("_AdjustAmount", adjustAmount);
            props.Add("_BeforeAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            props.Add("_AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            props.Add("_Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            _ = _mainDbClient.Execute("Admin_AdjustmentActualDeposit_Set", trans , props);
            
            var beforeAmount = props.Get<decimal>("_BeforeAmount");
            var afterAmount = props.Get<decimal>("_AfterAmount");
            var resultCode = props.Get<int>("_Result");

            if (resultCode != 0)
            {
                trans.Rollback();
                conn.Close();
                conn.Dispose();
                return resultCode;
            }
            
            props = new DynamicParameters();
            props.Add("_TransId", transId);
            props.Add("_DepositId", depositId);
            props.Add("_AdjustType", (int) AdjustWalletTypeEnum.ThirdParty);
            props.Add("_BeforeAmount", beforeAmount);
            props.Add("_AfterAmount", afterAmount);
            
            if (_mainDbClient.Execute("Admin_AdjustmentDepositLog_Ins", trans ,props) < 1)
            {
                trans.Rollback();
                conn.Close();
                conn.Dispose();
                return resultCode;
            }

            trans.Commit();
            conn.Close();
            conn.Dispose();
            return 0;
        }
    }
}
