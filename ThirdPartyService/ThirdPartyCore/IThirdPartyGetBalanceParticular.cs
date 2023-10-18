using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface IThirdPartyGetBalanceParticular
    {
        /// <summary>
        /// 取得指定第三方多筆客戶餘額
        /// </summary>
        /// <param name="inputs">第三方相關參數</param>
        /// <param name="tpUserNameString">第三方使用者名稱參數清單</param>
        /// <param name="tpCurrencyId">第三方幣別</param>
        /// <returns></returns>
        ApiResult<List<ReceiveTpUserBalanceModel>> GetBalance(Dictionary<string, string> inputs, string tpUserNameString, string tpCurrencyId);
    }
}
