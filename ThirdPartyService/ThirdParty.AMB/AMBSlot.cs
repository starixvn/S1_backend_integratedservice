using Newtonsoft.Json;

namespace ThirdParty.AMB
{
    public class AMBSlot : AMB
    {
        private CurrencyDataAMB SlotCurrencyData { get; set; }

        public AMBSlot(AMBConfig inputs, string tpCurrency)
        {
            Config = inputs;
            SlotCurrencyData = GetSlotCurrencyData(tpCurrency);
        }

        public override GameListResponse GetGameList(Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{SlotCurrencyData.ApiUrl}/seamless/list-games";
            var param = new
            {
                agent = SlotCurrencyData.Agent,
            };
            var jsonParam = JsonConvert.SerializeObject(param);
            try
            {
                var result = callback(apiUrl, jsonParam, "application/json", null, 0);
                var games = JsonConvert.DeserializeObject<GameListResponse>(result);
                return games;
            }
            catch (Exception e)
            {
                return new GameListResponse()
                {
                    status = new Status()
                    {
                        code = 999,
                        message = e.Message
                    }
                };
            }
        }

        public override CommonResponse CreateMember(string tpUserName, Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{SlotCurrencyData.ApiUrl}/seamless/create";
            var param = new
            {
                username = tpUserName,
                agent = SlotCurrencyData.Agent,
            };

            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-ambslot-signature", HMACSHA512(jsonParam, SlotCurrencyData.SecretKey)}
            };


            try
            {
                var content = callback(apiUrl, jsonParam, "application/json", header, 0);
                return JsonConvert.DeserializeObject<CommonResponse>(content);
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
            var apiUrl = $"{SlotCurrencyData.ApiUrl}/seamless/launch";
            if (isFun)
            {
                apiUrl = $"{apiUrl}/demo";
                var param = new
                {
                    gameId = gameId,
                    backLink = homeUrl,
                    agent = SlotCurrencyData.Agent,
                };
                var jsonParam = JsonConvert.SerializeObject(param);
                var header = new Dictionary<string, string>
                {
                    { "Accept-Language",tpLangId},
                    {"x-ambslot-signature", HMACSHA512(jsonParam, SlotCurrencyData.SecretKey)}
                };

                try
                {
                    var content = callback(apiUrl, jsonParam, "application/json", header, 0);
                    var result = JsonConvert.DeserializeObject<DemoUrlResponse>(content);
                    return new CommonResponse()
                    {
                        status = result.status,
                        data = new Data() { url = result.data }
                    };
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
            else
            {
                var param = new
                {
                    username = tpUserName,
                    gameId = gameId,
                    backLink = homeUrl,
                    agent = SlotCurrencyData.Agent,
                };
                var jsonParam = JsonConvert.SerializeObject(param);
                var header = new Dictionary<string, string>
                {
                    {"x-ambslot-signature", HMACSHA512(jsonParam, SlotCurrencyData.SecretKey)}
                };

                try
                {
                    var content = callback(apiUrl, jsonParam, "application/json", header, 0);
                    return JsonConvert.DeserializeObject<CommonResponse>(content);
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
        }

        public override GameReportResponse GetRoundResult(string tpUserName, string roundId, string gameId, string product, string type, Func<string, string, string, Dictionary<string, string>, int, string> callback)
        {
            var apiUrl = $"{SlotCurrencyData.ApiUrl}/seamless/result";
            var param = new
            {
                tranId = roundId,
                agent = SlotCurrencyData.Agent,
                username = tpUserName,
                language = "en",
                provider = product.ToUpper(),
                type = type
            };
            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-ambslot-signature", HMACSHA512(jsonParam, SlotCurrencyData.SecretKey)}
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