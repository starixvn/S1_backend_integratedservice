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

    /// <summary>
    /// 指定新增TpSeamlessBonus的方式
    /// </summary>
    public enum SetTpSeamlessBonusType
    {
        /// <summary>
        /// 新增Bonus使用紀錄，有存在BonusId資料會INSERT IGNORE，執行 Service_TpSeamlessBonus_Ins
        /// </summary>
        Insert,
        /// <summary>
        /// 新增Bonus使用紀錄或更新資料，有存在BonusId資料會Update，執行 Service_TpSeamlessBonus_Set
        /// </summary>
        Update,
    }

}
