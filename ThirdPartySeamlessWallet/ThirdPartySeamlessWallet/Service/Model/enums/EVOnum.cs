using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{

    public enum EVOStatus
    {
        OK,
        TEMPORARY_ERROR,
        INVALID_TOKEN_ID,
        INVALID_SID,
        ACCOUNT_LOCKED,
        FATAL_ERROR_CLOSE_USER_SESSION,
        UNKNOWN_ERROR,
        INVALID_PARAMETER,
        BET_DOES_NOT_EXIST,
        BET_ALREADY_EXIST,
        BET_ALREADY_SETTLED,
        INSUFFICIENT_FUNDS,
        FINAL_ERROR_ACTION_FAILED,
        GEOLOCATION_FAIL,
        BONUS_LIMIT_EXCEEDED,

        //Required only for MGA
        CASINO_LIMIT_EXCEEDED_TURNOVER,
        CASINO_LIMIT_EXCEEDED_SESSION_TIME,
        CASINO_LIMIT_EXCEEDED_STAKE,
        CASINO_LIMIT_EXCEEDED_LOSS,

        //Required only for AAMS
        SESSION_ALREADY_CLOSED,
        SESSION_DOES_NOT_EXIST,
        OPERATION_IN_PROCESS

    }

    public enum PaidFromEnum
    {
        NVC = 1,
        TP = 2
    }

    public enum PromoTransactionTypeEnum
    {
        JackpotWin,
        FreeRoundPlayableSpent,
        RewardGamePlayableSpent,
        RewardGameWinCapReached,
        RewardGameMinBetLimitReached,
        FromGame,
        RtrMonetaryReward
    }
}
