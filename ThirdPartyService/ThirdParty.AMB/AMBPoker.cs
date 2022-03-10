using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ThirdParty.AMB
{
    public class AMBPoker : AMB
    {
        public AMBPoker(IReadOnlyDictionary<string, string> inputs)
        {
            Setting = new AMBSetting()
            {
                ApiUrl = inputs["ApiUrl"], 
                Agent = inputs["Agent"], 
                SecretKey = inputs["SecretKey"]
            };
        }

        public override GameListResponse GetGameList(Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            throw new NotImplementedException();
        }

        public override string CreateMember(string tpUserName, Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{Setting.ApiUrl}/seamless/create";
            var param = new
            {
                username = tpUserName,
                agent = Setting.Agent,
            };

            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-amb-signature", HMACSHA512(JsonConvert.SerializeObject(param), Setting.SecretKey)}
            };
            return callback(apiUrl, jsonParam, "application/json", header, 0);
        }

        public override string LaunchSelectedGame(string tpUserName, string gameId, string homeUrl, bool isFun,
            Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{Setting.ApiUrl}/seamless/launch";

            var param = new
            {
                username = tpUserName,
                agent = Setting.Agent,
            };
            
            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-amb-signature", HMACSHA512(jsonParam, Setting.SecretKey)}
            };

            return callback(apiUrl, jsonParam, "application/json", header, 0);
        }

        public override GameReportResponse GetRoundResult(string tpUserName, string roundId, string gameId, Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{Setting.ApiUrl}/seamless/report";
            var param = new
            {
                username = tpUserName,
                roundId,
                agent = Setting.Agent,
                language = "en",
            };

            var jsonParam = JsonConvert.SerializeObject(param); 
            
            var header = new Dictionary<string, string>
            {
                { "x-amb-signature", HMACSHA512(jsonParam, Setting.SecretKey) }
            };
            var result = callback(apiUrl, jsonParam, "application/json", header, 0);
            
            try
            {
                return JsonConvert.DeserializeObject<GameReportResponse>(result);
            }
            catch (Exception e)
            {
                return new GameReportResponse()
                {
                    status = new Status()
                    {
                        code = 997,
                        message = result
                    }
                };
            }
        }
    }
}