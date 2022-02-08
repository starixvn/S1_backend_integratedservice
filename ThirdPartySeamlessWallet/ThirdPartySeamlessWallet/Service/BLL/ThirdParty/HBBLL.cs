using System;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Controllers;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class HBBLL : IHBBLL
    {
        private readonly Tp thisTp;
        public readonly GameType GameType;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;

        public HBBLL(AuthBLL authBll, ThirdPartyBLL thirdPartyBll, TpCustInfoBLL tpCustInfoBll)
        {
            thisTp = Tp.HB;
            GameType = GameType.HB_RNG;
            _authBLL = authBll;
            _thirdPartyBLL = thirdPartyBll;
            _tpCustInfoBLL = tpCustInfoBll;
        }

        public HBPlayerDetailResponse Auth(HBPlayerDetailRequest request)
        {
            var response = new HBPlayerDetailResponse()
            {
                playerdetailresponse = new playerdetailresponse()
            };
            var status = new HBStatus()
            {
                success = false,
                autherror = true
            };
            
            if (!string.IsNullOrEmpty(request.playerdetailrequest.token))
            {
                try
                {
                    var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(request.playerdetailrequest.token)));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    var authToken = sessionKeyArr[0]; /* 1 Time*/
                    var sessionToken = sessionKeyArr[1];/* Long Time*/
                    if (_authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    {
                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                        if (tpCheck == null || tpCheck.Token != sessionToken)
                        {
                            status.message = "Player session is expired";
                        }
                        else if (tpCheck.Locked)
                        {
                            status.message = "Player is inactive";
                        }
                        else
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, (int)GameType);
                            if (tpCustBalance != null)
                            {
                                response.playerdetailresponse.Status = new HBStatus()
                                {
                                    success = true,
                                    autherror = false,
                                    message = ""
                                };
                                response.playerdetailresponse.accountid = tpCustInfoAuth.TpUserName;
                                response.playerdetailresponse.accountname = tpCustInfoAuth.TpUserName;
                                response.playerdetailresponse.balance = tpCustBalance.Balance;
                                response.playerdetailresponse.currencycode = tpCustBalance.TpCurrencyId;
                                return response;
                            }
                        }
                    }
                    else
                    {
                        status.message = "Invalid player session";
                    }
                }
                catch (FormatException fEx)
                {
                    
                }
            }
            else
            {
                status.message = "Player session token is empty";
            }
            response.playerdetailresponse.Status = status;
            return response;
        }
    }
}