namespace ThirdPartySeamlessWallet.Service.Model.enums
{

    /// <summary>
    /// 指定查詢TpSeamlessBonus的方式
    /// </summary>
    public enum GetTpSeamlessBonusType
    {
        /// <summary>
        /// 使用TpId, GameType, TransId 進行查詢
        /// </summary>
        ByGameTypeAndTransId,
        /// <summary>
        /// 僅使用TpId, TransId 進行查詢
        /// </summary>
        ByTransId,
    }

}
