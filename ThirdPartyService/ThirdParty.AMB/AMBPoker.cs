using Newtonsoft.Json;

namespace ThirdParty.AMB
{
    public class AMBPoker : AMB
    {
        private CurrencyDataAMB PokerCurrencyData { get; set; }

        public AMBPoker(AMBConfig inputs, string tpCurrency)
        {
            Config = inputs;
            PokerCurrencyData = GetPokerCurrencyData(tpCurrency);
        }

        public override GameListResponse GetGameList(Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            throw new NotImplementedException();
        }

        public override CommonResponse CreateMember(string tpUserName, Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{PokerCurrencyData.ApiUrl}/seamless/create";
            var param = new
            {
                username = tpUserName,
                agent = PokerCurrencyData.Agent,
            };

            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-amb-signature", HMACSHA512(JsonConvert.SerializeObject(param), PokerCurrencyData.SecretKey)}
            };

            try
            {
                var response = callback(apiUrl, jsonParam, "application/json", header, 0);
                return JsonConvert.DeserializeObject<CommonResponse>(response);
            }
            catch (Exception e)
            {
                return new CommonResponse()
                {
                    status = new Status()
                    {
                        code = 999,
                        message = e.Message
                    }
                };
            }
        }

        public override CommonResponse LaunchGame(string tpUserName, string gameId, string homeUrl, bool isFun, string tpLangId,
            Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{PokerCurrencyData.ApiUrl}/seamless/launch";

            var param = new
            {
                username = tpUserName,
                agent = PokerCurrencyData.Agent,
            };

            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-amb-signature", HMACSHA512(jsonParam, PokerCurrencyData.SecretKey)}
            };

            try
            {
                var content = callback(apiUrl, jsonParam, "application/json", header, 0);
                var response = JsonConvert.DeserializeObject<CommonResponse>(content);
                response.data.url += "&e=disable&h=disable";
                return response;
            }
            catch (Exception e)
            {
                return new CommonResponse()
                {
                    status = new Status()
                    {
                        code = 999,
                        message = e.Message
                    }
                };
            }
        }

        public override GameReportResponse GetRoundResult(string tpUserName, string roundId, string gameId, string product, string type, Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{PokerCurrencyData.ApiUrl}/seamless/report";
            var param = new
            {
                username = tpUserName,
                roundId,
                agent = PokerCurrencyData.Agent,
                language = "en",
            };

            var jsonParam = JsonConvert.SerializeObject(param);

            var header = new Dictionary<string, string>
            {
                { "x-amb-signature", HMACSHA512(jsonParam, PokerCurrencyData.SecretKey) }
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