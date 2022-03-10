using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ThirdParty.AMB
{
    public class AMBSlot :AMB
    {
        public AMBSlot(IReadOnlyDictionary<string, string> inputs)
        {
            Setting = new AMBSetting()
            {
                ApiUrl = inputs["SlotApiUrl"], 
                Agent = inputs["SlotAgent"], 
                SecretKey = inputs["SlotSecretKey"]
            };
        }

        public override GameListResponse GetGameList(Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{Setting.ApiUrl}/seamless/list-games";
            var param = new
            {
                agent = Setting.Agent,
            };
            var jsonParam = JsonConvert.SerializeObject(param);
            var resulu = callback(apiUrl, jsonParam, "application/json", null, 0);
            var games = JsonConvert.DeserializeObject<GameListResponse>(resulu);
            return games;
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
                {"x-ambslot-signature", HMACSHA512(jsonParam, Setting.SecretKey)}
            };
            return callback(apiUrl, jsonParam, "application/json", header, 0);
        }

        public override string LaunchSelectedGame(string tpUserName, string gameId, string homeUrl, bool isFun,
            Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{Setting.ApiUrl}/seamless/launch";
            if (isFun)
            {
                var games = GetGameList(callback);
                if (games.status.code == 0)
                {
                    var game = games.data.SingleOrDefault(a => a.prefix.Trim() == gameId.Trim());
                    if (game != null)
                    {
                        var funGame = new
                        {
                            status = new Status()
                            {
                                code = games.status.code,
                                message = games.status.message
                            },
                            data = new
                            {
                                url = game.demoUrl
                            }
                        };
                        return JsonConvert.SerializeObject(funGame);   
                    }
                    var noGame = new
                    {
                        status = new Status()
                        {
                            code = 997,
                            message = "no games"
                        }
                    };
                    return JsonConvert.SerializeObject(noGame);   
                }
                else
                {
                    var funGame = new
                    {
                        status = new Status()
                        {
                            code = games.status.code,
                            message = games.status.message
                        }
                    };
                    return JsonConvert.SerializeObject(funGame);   
                }
            }
            else
            {
                var param = new
                {
                    username = tpUserName,
                    gameId = gameId,
                    backLink = homeUrl,
                    agent = Setting.Agent,
                };
                var jsonParam = JsonConvert.SerializeObject(param);
                var header = new Dictionary<string, string>
                {
                    {"x-ambslot-signature", HMACSHA512(jsonParam, Setting.SecretKey)}
                };
                return callback(apiUrl, jsonParam, "application/json", header, 0);
            }
        }

        public override GameReportResponse GetRoundResult(string tpUserName, string roundId, string gameId, Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            throw new NotImplementedException();
        }
    }
}