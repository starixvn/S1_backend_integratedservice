using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public class AG_EnquireWorkerService : BackgroundService
    {
        private Timer timer;
        private readonly ILogger<AG_EnquireWorkerService> logger;

        public AG_EnquireWorkerService(ILogger<AG_EnquireWorkerService> logger)
        {
            this.logger = logger;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            //IEnumerable<TpApiFundTransfer> tpApiFundTransferList = null;
            //try
            //{
            //    using (SqlConnection conn = new SqlConnection(sqlHelper.ConnectString))
            //    {
            //        tpApiFundTransferList = conn.Query<TpApiFundTransfer>("SELECT T.FundTransferID, T.APIType, T.ThirdPartyID, T.UserID, T.Direction, T.TransferAmount, T.BeforeAmount, T.AfterAmount, T.Status, T.AddTime, A.LoginUserName, A.ApiUserName, Ta.JSONstr, Ta.DisplayName, Ta.ApiName FROM dbo.TpApiFundTransfer AS T WITH (NOLOCK) INNER JOIN  dbo.ApiUserInfo AS A WITH (NOLOCK) ON T.UserID = A.UserId AND T.APIType = A.ApiType INNER JOIN dbo.TpApiSetting AS Ta WITH (NOLOCK) ON A.ApiType = Ta.APIType WHERE (T.Status IN (2, 3)) AND (DATEADD(minute, 3, T.AddTime) < GETDATE())");
            //    }

            //    if (tpApiFundTransferList != null)
            //    {
            //        foreach (var tpApiFundTransfer in tpApiFundTransferList)
            //        {
            //            try
            //            {
            //                if (tpApiFundTransfer.Direction == TransDirection.Main2ThirdParty && tpApiFundTransfer.Status == TransferStatus.Pending)
            //                {
            //                    using (SqlConnection conn = new SqlConnection(sqlHelper.ConnectString))
            //                    {
            //                        DynamicParameters p = new DynamicParameters();
            //                        p.Add("@FundTransferID", tpApiFundTransfer.FundTransferID);
            //                        p.Add("@Memo", "异常");
            //                        p.Add("@ForStatus", (int)TransferStatus.Pending);
            //                        p.Add("@Status", (int)TransferStatus.Fail);
            //                        conn.Execute("dbo.[TpApiFundTransfer_UpdStatus]", p, commandType: CommandType.StoredProcedure);
            //                    }
            //                }

            //                var refThirdPartyHelper = thirdPartyHelper.GetThirdPartyHelper(tpApiFundTransfer.APIType);
            //                if (!refThirdPartyHelper.isHeathly)
            //                {
            //                    continue;
            //                }

            //                ApiUserInfo oApiUserInfo = new ApiUserInfo();
            //                oApiUserInfo.ApiType = tpApiFundTransfer.APIType;
            //                oApiUserInfo.UserId = tpApiFundTransfer.UserID;
            //                oApiUserInfo.LoginUserName = tpApiFundTransfer.LoginUserName;
            //                oApiUserInfo.ApiUserName = tpApiFundTransfer.ApiUserName;

            //                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(tpApiFundTransfer.JSONstr);
            //                var oApiResult = refThirdPartyHelper.CheckTransfer(oApiUserInfo, inputs, tpApiFundTransfer.FundTransferID, tpApiFundTransfer.ThirdPartyID, tpApiFundTransfer.Direction);
            //                TpTransfer tpTransfer = new TpTransfer();

            //                tpTransfer.Amount = tpApiFundTransfer.TransferAmount;
            //                tpTransfer.apiType = tpApiFundTransfer.APIType;
            //                tpTransfer.direction = tpApiFundTransfer.Direction;
            //                tpTransfer.inputs = inputs;
            //                tpTransfer.UserId = tpApiFundTransfer.UserID;

            //                if (oApiResult.ErrorCode == ErrorCode.TransferNotExist && tpApiFundTransfer.AddTime.AddMinutes(10) > DateTime.Now)
            //                {
            //                    continue;
            //                }

            //                qKDAL.DealTranscation(oApiResult, tpTransfer, tpApiFundTransfer.FundTransferID);
            //            }
            //            catch (SqlException SqlEx)
            //            {
            //                StringBuilder errorMessages = new StringBuilder();

            //                for (int i = 0; i < SqlEx.Errors.Count; i++)
            //                {
            //                    var SqlExErrors = SqlEx.Errors[i];
            //                    errorMessages.AppendLine($"Index #{i}");
            //                    errorMessages.AppendLine($"Message: {SqlExErrors.Message}");
            //                    errorMessages.AppendLine($"Error Number: {SqlExErrors.Number}");
            //                    errorMessages.AppendLine($"LineNumber: {SqlExErrors.LineNumber}");
            //                    errorMessages.AppendLine($"Source: {SqlExErrors.Source}");
            //                    errorMessages.AppendLine($"Procedure: {SqlExErrors.Procedure}");
            //                }
            //                loggerHelper.Error($"错误信息：{errorMessages.ToString()}", "BetHistoryWorkerService");
            //            }
            //            catch (Exception ex)
            //            {
            //                loggerHelper.Error(ex.Message, "WorkerService");
            //            }

            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    loggerHelper.Error(ex.Message, "WorkerService");
            //}
            //finally
            //{
            //    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
            //    if (environment.IsDevelopment() || environment.IsEnvironment("QA"))
            //    {
            //        dueTimeSpan = TimeSpan.FromSeconds(5);
            //    }
            //    timer.Change(dueTimeSpan, TimeSpan.Zero);
            //}
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            logger.LogInformation("WriteToFileHostedService: Process Stopped", "WorkerService");

            return Task.CompletedTask;
        }
    }
}
