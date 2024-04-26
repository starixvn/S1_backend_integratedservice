using System.Security.Cryptography;
using System.Text;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.AMB
{
    public static class AMBFactory
    { 
        public static AMB Create(TpGameType gameType, AMBConfig inputs, string tpCurrency)
        {
            return gameType switch
            {
                TpGameType.AMB_Poker => new AMBPoker(inputs),
                TpGameType.AMB_RNG => new AMBSlot(inputs, tpCurrency),
                _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
            };
        }
    }

    public abstract class AMB
    {
        protected AMBConfig Config;

        public abstract GameListResponse GetGameList(Func<string, string, string, Dictionary<string, string>, int, string> callback);
        public abstract CommonResponse CreateMember(string tpUserName, Func<string, string, string, Dictionary<string, string>, int, string> callback);

        public abstract CommonResponse LaunchGame(string tpUserName, string gameId, string homeUrl, bool isFun,string tpLangId,
            Func<string, string, string, Dictionary<string, string>, int, string> callback);

        public abstract GameReportResponse GetRoundResult(string tpUserName, string roundId, string gameId,
            Func<string, string, string, Dictionary<string, string>, int, string> callback);

        protected string HMACSHA512(string input, string key)
        {
            var salt = Encoding.UTF8.GetBytes(key);
            var HASH_SIZE = 64;
            var ITERATIONS = 1000;

            // Generate the hash
            var pbkdf2 = new Rfc2898DeriveBytes(input, salt, ITERATIONS, HashAlgorithmName.SHA512);
            var data = pbkdf2.GetBytes(HASH_SIZE);
            return Convert.ToBase64String(data);
        }
        protected SlotCurrencyAgent GetSlotCurrencyConfig(string currencyId)
        {
            return Config.SlotCurrencyAgent.FirstOrDefault(x => x.CurrencyId == currencyId);
        }
    }
}