using System;
using System.Collections.Generic;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using System.Xml;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using System.Linq;
using Newtonsoft.Json;

namespace ThirdParty.AG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        Dictionary<string, string> oddsTypeDic = new Dictionary<string, string>() {
            {"MYR","GF4B" },
            {"CNY","C" },
            {"THB","GF4A" },
            {"IDR","GF4A" },
            {"VND ","GF4A" },
            {"USD ","GF4A" },
            {"INR ","GF4A" }
        };

        public class result
        {
            public string info { get; set; }
            public string msg { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string actype = tpCustInfo.IsTest ? "0" : "1";
            string cagent = inputs.ContainsKey("cagent") && !string.IsNullOrEmpty(inputs["cagent"]) ? inputs["cagent"] : throw new Exception();
            string apiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string Md5key = inputs.ContainsKey("Md5key") && !string.IsNullOrEmpty(inputs["Md5key"]) ? inputs["Md5key"] : throw new Exception();
            
            string _oddtype = null;
            oddsTypeDic.TryGetValue(tpCustInfo.TpCurrencyId, out _oddtype);
            string oddtype = _oddtype;

            string sParams = $"cagent={cagent}/\\\\/loginname={tpCustInfo.TpUserName}/\\\\/method=lg/\\\\/actype={actype}/\\\\/password={tpCustInfo.TpPwd}/\\\\/oddtype={oddtype}/\\\\/cur={tpCustInfo.TpCurrencyId}";
            string eParams = DESEncrypt(sParams, inputs["Deskey"]);
            string oMD5 = $"{eParams}{Md5key}";
            string postParams = $"params={eParams}&key={ ToMD5(oMD5).ToLower() }";

            Dictionary<string, string> header = new Dictionary<string, string>();
            header["User-Agent"] = $"WEB_LIB_GI_{cagent}";

            string sResponse = HttpGet(apiUrl, postParams, "application/x-www-form-urlencoded", header);
            ApiResult oApiResult = new ApiResult();

            if (IsXML(sResponse))
            {
                var oCreateMemberResp = FromXml<result>(sResponse);
                switch (oCreateMemberResp.info.ToString())
                {
                    case "0":
                        oApiResult.ErrorCode = ErrorCode.Success;
                        oApiResult.Message = oCreateMemberResp.msg;
                        break;
                    case "key_error":
                        oApiResult.ErrorCode = ErrorCode.KEY_ERROR;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "network_error":
                        oApiResult.ErrorCode = ErrorCode.NETWORK_ERROR;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "account_add_fail":
                        oApiResult.ErrorCode = ErrorCode.Fail;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "error":
                        oApiResult.ErrorCode = ErrorCode.NoAuthority;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    default:
                        oApiResult.ErrorCode = ErrorCode.NoAuthority;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = sResponse;
            }

            return oApiResult;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.ThirdId = $"";
            transIdModel.Implemented = false;

            return transIdModel;

        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string actype = tpCustInfo.IsTest ? "0" : "1";
            string cagent = inputs.ContainsKey("cagent") && !string.IsNullOrEmpty(inputs["cagent"]) ? inputs["cagent"] : throw new Exception();
            string data_cagent = inputs.ContainsKey("data_cagent") && !string.IsNullOrEmpty(inputs["data_cagent"]) ? inputs["data_cagent"] : throw new Exception();
            string Md5key = inputs.ContainsKey("Md5key") && !string.IsNullOrEmpty(inputs["Md5key"]) ? inputs["Md5key"] : throw new Exception();
            string SessionUrl = inputs.ContainsKey("SessionUrl") && !string.IsNullOrEmpty(inputs["SessionUrl"]) ? inputs["SessionUrl"] : throw new Exception();
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();

            string _oddtype = null;
            oddsTypeDic.TryGetValue(tpCustInfo.TpCurrencyId, out _oddtype);
            string oddtype = _oddtype;

            //string oddtype = "";
            string balance = "";
            string token = "";
            inputs.TryGetValue("Balance", out balance);
            inputs.TryGetValue("Token", out token);
            string postParams = $"productid={data_cagent}&username={tpCustInfo.TpUserName}&session_token={token}&credit={balance}";
            Dictionary<string, string> header = new Dictionary<string, string>();
            header["User-Agent"] = $"WEB_LIB_GI_{cagent}";

            string sResponse = HttpGet(SessionUrl, postParams, "application/x-www-form-urlencoded", header);
            ApiResult oApiResult = new ApiResult();
            {
                if (IsXML(sResponse))
                {
                    var ResponseCode = FromXmlNode(sResponse);
                    switch (ResponseCode.ToString())
                    {
                        case "OK":
                            break;
                        default:
                            oApiResult.ErrorCode = ErrorCode.ServerResponseError;
                            oApiResult.RawMessage = ResponseCode;
                            return oApiResult;
                    }
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = sResponse;
                    return oApiResult;
                }
            }


            long sTimestemp = GetTimestemp1();

            string dm = "www.nova88.net";
            if (inputs.ContainsKey("host"))
            {
                dm = inputs["host"];
            }

            string sParams = $"cagent={cagent}/\\\\/loginname={tpCustInfo.TpUserName}/\\\\/password={tpCustInfo.TpPwd}/\\\\/actype={actype}/\\\\/oddtype={oddtype}/\\\\/cur={tpCustInfo.TpCurrencyId}/\\\\/dm={dm}/\\\\/sid={cagent}{sTimestemp}/\\\\/lang=1";

            if (inputs.ContainsKey("gameType") && !string.IsNullOrEmpty(inputs["gameType"]))
            {
                sParams = $"{sParams}/\\\\/gameType={inputs["gameType"]}";
            }

            string eParams = DESEncrypt(sParams, inputs["Deskey"]);
            string oMD5 = $"{eParams}{Md5key}";
            postParams = $"params={eParams}&key={ ToMD5(oMD5).ToLower() }";

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}?{postParams}";
            return oApiResult;
        }


        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var billNoList = TpBetRecordProvider.ReportDbQuery("Service_BettransLC_AGV2_Payout_Get", new { transactionID = getBetDetail.TransId })
                .Select(item => item.GetValueOrDefault("billNo")?.ToString());

            var wagerDetailList = TpBetRecordProvider.WagerDbQuery("Service_TpWager_AG_Detail_Get", 
                new { billNoList = JsonConvert.SerializeObject(billNoList) });

            var detail = ToJson(wagerDetailList);
            var gameCode = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("gameType")?.ToString();
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("gameCode")?.ToString(),
                    TpUserName = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("playName")?.ToString(),
                    UTCBetTime = wagerDetailList.Min(i => (DateTime)i.GetValueOrDefault("betTime", DateTime.MinValue)),
                    UTCSettledTime = wagerDetailList.Max(i => (DateTime)i.GetValueOrDefault("recalcuTime", DateTime.MinValue)),
                    Stake = wagerDetailList.Sum(i => (decimal)i.GetValueOrDefault("betAmount")) * tpCurrency.ExchangeRate,
                    WinlostAmount = wagerDetailList.Sum(i => (decimal)i.GetValueOrDefault("netAmount")) * tpCurrency.ExchangeRate,
                    GameCode = gameCode,
                    GameName = GetGameName(gameCode),
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }
        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "BAC" => "百家乐",
                "CBAC" => "包桌百家乐",
                "LINK" => "连环百家乐",
                "DT" => "龙虎",
                "SHB" => "骰宝",
                "ROU" => "轮盘",
                "FT" => "番摊",
                "LBAC" => "竞咪百家乐",
                "ULPK" => "终极德州扑克",
                "SBAC" => "保险百家乐",
                "NN"=> "牛牛",
                "BJ" => "Blackjack",
                "ZJH" => "炸金花",
                "BF" => "斗牛",
                "SG" => "三公",
                _ => string.Empty,
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        protected T FromXml<T>(string xml) where T : new()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlElement root = doc.DocumentElement;

            XmlAttributeCollection nodeList = root.Attributes;

            var data = new T();

            foreach (var key in data.GetType().GetProperties())
            {
                key.SetValue(data, nodeList.GetNamedItem(key.Name).Value);

            }
            return data;
        }

        protected string FromXmlNode(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlElement root = doc.DocumentElement;
            XmlAttributeCollection nodeList = root.Attributes;
            XmlNodeList nodes = doc.DocumentElement.SelectNodes("/PlayerTicketResponse");
            return nodes[0].SelectSingleNode("ResponseCode").InnerText; ;
        }
    }
}
