using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        #region BTI

        public async Task<BTIResult> Reserve_BTIAsync(TpCustInfo tpCustInfo, BTIRequestObj request)
        {         
            try
            {
                var bonusId = 0;
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new
                {
                    request.cust_id,
                    request.reserve_id,
                    request.amount,
                    TicketType = (int)BettransType.Reserve,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);
                _ = await conn.ExecuteAsync("Service_BettransSports_BTI_Reserve_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                
                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    Trx_id = p.Get<long>("trx_id"),
                };
                if (btiResult.SPResult < 1)
                {
                    return btiResult;
                }

                btiResult.SetBalanceResult = PlaceBetV2(
                    tpCustInfo: tpCustInfo,
                    amount: Math.Abs(request.amount) * -1,
                    tpId: (int)Tp.BTI,
                    tpTransId: request.reserve_id,
                    gameType:(int)GameType.BTI_Sports,
                    refId: request.reserve_id,
                    betData: null,
                    tpCategory: TpCategory.Sports,
                    afterAmount: out decimal afterAmount,
                    bonusId: ref bonusId,
                    conn: conn,
                    trans: trans
                );

                btiResult.AfterAmount = afterAmount;
                if (btiResult.SetBalanceResult == 1)
                {                  
                    trans.Commit();    
                }

                return btiResult;
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    return new BTIResult();
                }
                else
                {
                    throw mySqlEx;
                }
            }
        }

        public async Task<BTIResult> DebitReserve_BTIAsync(TpCustInfo tpCustInfo, BTIDebitRequest debitReserve)
        {     
            try
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                
                var p = new DynamicParameters(debitReserve);
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                _ = await conn.ExecuteAsync("Service_BettransSports_BTI_DebitReserve_Ins", p , transaction: trans, commandType: CommandType.StoredProcedure);

                var btiResult = new BTIResult()
                {
                    Trx_id = p.Get<long>("trx_id"),
                };

                if (btiResult.Trx_id > 0)
                {
                    trans.Commit();
                    btiResult.SPResult = 1;
                }
                return btiResult;
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    return new BTIResult();
                }
                else
                {
                    throw mySqlEx;
                }
            }
        }

        #endregion
    }
}
