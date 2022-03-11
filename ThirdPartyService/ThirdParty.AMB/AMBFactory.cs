using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.AMB
{
    public static class AMBFactory
    {
        public static AMB Create(TpGameType gameType, Dictionary<string, string> inputs)
        {
            return gameType switch
            {
                TpGameType.AMB_Poker => new AMBPoker(inputs),
                TpGameType.AMB_RNG => new AMBSlot(inputs),
                _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
            };
        }
    }

    public abstract class AMB
    {
        protected class AMBSetting
        {
            public string ApiUrl { get; set; }
            public string Agent { get; set; }
            public string SecretKey { get; set; }
        }
        protected AMBSetting Setting;

        public abstract GameListResponse GetGameList(Func<string, string, string, Dictionary<string, string>, int, string> callback);
        public abstract CommonResponse CreateMember(string tpUserName, Func<string, string, string, Dictionary<string, string>, int, string> callback);

        public abstract CommonResponse LaunchGame(string tpUserName, string gameId, string homeUrl, bool isFun,
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
    }
}