using System.Text;

using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.SBSW

{
    public class SBSWDeepLinkHelper
    {
        public string GameUrl
        {
            get
            {
                return _gameUrl;
            }
        }

        private string _gameUrl { get; set; }

        private string _sportType { get; set; }
        private string _menuType { get; set; }
        private string _matchId { get; set; }
        private string _leagueId { get; set; }
        private string _betType { get; set; }
        private AppType _appType { get; set; }
        private bool _isFun { get; set; }

        private readonly Dictionary<string, string> menuTypeDic = new Dictionary<string, string>()
        {
            {"2001","1" },// Olympics
            {"2002","2" },// World Cup
            {"2003","3" } // Euro Cup
        };

        private readonly Dictionary<string, string> sportTypeDic = new Dictionary<string, string>()
        {
            {"1","1" },
            {"2","basketball" },
            {"3","football" },
            {"4","icehockey" },
            {"5","tennis" },
            {"6","volleyball" },
            {"8","baseball" } ,
            {"9","badminton" } ,
            {"50", "indiancricket" },
            {"161","161" },
            {"164","164" },
            {"171","vgaming" },
            {"180","180" },
            {"190","190" },
            {"193","193" },
            {"194","194" },
            {"197","197" },
            {"200","vgaming" },
            //{"200","sabaclub" },
            {"202","keno" },
            {"208","casino" },
            {"220","KenoLottery" },
            {"222","tablegame" },
            {"223","indo4d" },
            {"997","90088,89692,89697,93062,95730,96666,95731" }
        };

        private readonly Dictionary<string, Func<string>> _sportTypeHandler;

        public SBSWDeepLinkHelper(string sabaUrl, TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs, GameDetail gameDetail, SabaConfig config)
        {
            _sportTypeHandler = new Dictionary<string, Func<string>>
            {
                { "43", ESports},
                { "192", VirtualSoccerWorldCup},
                { "237", MaxGame},
                { "245", VirtualGames},
            };

            _isFun = tpCustInfo.TpUserName == null;
            _appType = appType;

            //設定前端傳入參數
            _sportType = inputs.GetValueOrDefault("gameId");
            _menuType = inputs.GetValueOrDefault("menutype") ?? "0";
            _matchId = inputs.GetValueOrDefault("matchid");
            _leagueId = inputs.GetValueOrDefault("leagueid");
            _betType = inputs.GetValueOrDefault("bettype");

            if (gameDetail != null)
            {
                _sportType = gameDetail.sport_type.ToString();
                _matchId = gameDetail.match_id.ToString();
                _leagueId = gameDetail.league_id.ToString();
            }

            // 共用參數
            var resultUrl = new StringBuilder(sabaUrl);
            resultUrl.Append($"{(_isFun ? "?" : "&")}lang={TpLangId}");
            var homeUrl = inputs.GetValueOrDefault("homeUrl");
            var signupUrl = inputs.GetValueOrDefault("signupUrl");
            var loginUrl = inputs.GetValueOrDefault("LoginUrl");
            resultUrl.Append($"&homeUrl={homeUrl}&signupUrl={signupUrl}&LoginUrl={loginUrl}");

            // 檢查sportType判斷走新的獨立Func或原本共用的Method
            var deeplink = _sportTypeHandler.ContainsKey(_sportType) ? _sportTypeHandler[_sportType].Invoke() :
                _appType switch
                {
                    AppType.Desktop => GetDesktopDeeplink(),
                    AppType.Mobile => GetMobileDeeplink(),
                    AppType.Native => GetNativeDeeplink(),
                    _ => string.Empty,
                };

            resultUrl.Append(deeplink);

            if (inputs.TryGetValue("isAPP", out var isAPP))
            {
                resultUrl.Append($"&isAPP={isAPP}");
            }

            if (config.WebSkinType != -1)
            {
                //如果有特殊參數則戴特殊參數，沒有則走原邏輯
                resultUrl.Append($"&WebSkinType={config.WebSkinType}");
            }

            if (config.Skin != -1)
            {
                //如果有特殊參數則戴特殊參數，沒有則走原邏輯
                resultUrl.Append($"&skin={config.Skin}");
            }
            else
            {
                if (inputs.TryGetValue("skin", out var skin))
                {
                    resultUrl.Append($"&skin={skin}");
                }
                else
                {
                    //vgaming的H5強制加skin = 0
                    if (_appType == AppType.Mobile && _sportType == "171")
                    {
                        resultUrl.Append($"&skin=0");
                    }
                }
            }

            _gameUrl = resultUrl.ToString();
        }

        private bool CheckSportType()
        {
            return _sportType switch
            {
                "171" => true,  // Vgaming
                "200" => true,  // 沙巴俱乐部
                "202" => true,  // 快乐彩
                "220" => true,  // 彩票(Keno Lottery)
                "222" => true,  // 桌面游戏
                "223" => true,  // Togel 4D
                _ => false,
            };
        }

        private string GetDesktopDeeplink()
        {
            var act = _sportType != null ? sportTypeDic.GetValueOrDefault(_sportType) : null;

            if (_sportType == "2001" || _sportType == "2002" || _sportType == "2003")
            {
                _menuType = menuTypeDic[_sportType];
                act = "1";
            }

            if (CheckSportType())
            {
                if (!string.IsNullOrEmpty(_betType))
                {
                    return $"&game={act}&gameid={_betType}";
                }
                if (_sportType == "997")
                {
                    // Saba Soccer deeplink
                    return $"&menutype=0&sportid=1&leaguekey={act}";
                }
                return $"&game={act}";
            }

            if (!string.IsNullOrEmpty(_matchId) && _matchId != "0")
            {
                return $"&act={_sportType}&matchid={_matchId}&sportid={_sportType}&leaguekey={_leagueId}";
            }

            if (!string.IsNullOrEmpty(_betType))
            {
                return $"&act={act}&gameid={_betType}&menutype={_menuType}";
            }

            if (_sportType == "997")
            {
                // Saba Soccer deeplink
                return $"&menutype=0&act={_sportType}";
            }

            return $"&act={act}&menutype={_menuType}";
        }

        private string GetMobileDeeplink()
        {
            if (_sportType == "2001" || _sportType == "2002" || _sportType == "2003")
            {
                _menuType = menuTypeDic[_sportType];
                _sportType = "1";
            }
            var types = CheckSportType() ? sportTypeDic.GetValueOrDefault(_sportType) : $"{_sportType},{_menuType},l";
            if (_sportType == "202")
            {
                types = "Keno";
            }
            if (_sportType == "208")
            {
                types = "CL";
            }

            var result = $"&types={types}&isChinaView=true";

            if (!string.IsNullOrEmpty(_matchId) && _matchId != "0")
            {
                return $"&matchid={_matchId}&sportid={_sportType}&leaguekey={_leagueId}";
            }

            if (!string.IsNullOrEmpty(_betType))
            {
                return $"{result}&gameid={_betType}";
            }

            return result;
        }

        private string GetNativeDeeplink()
        {
            if (!string.IsNullOrEmpty(_matchId) && _matchId != "0")
            {
                return $"&matchid={_matchId}";
            }

            if (!string.IsNullOrEmpty(_betType))
            {
                return $"&gameid={_betType}";
            }

            return string.Empty;
        }


        #region SportTypeHandler

        /// <summary>
        /// MaxGame
        /// </summary>
        /// <returns></returns>
        private string MaxGame()
        {
            if (_isFun)
            {
                return string.Empty;
            }

            return _appType switch
            {
                AppType.Desktop => "&game=MaxGame",
                AppType.Mobile => "&types=maxgame",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 虛擬娛樂城
        /// </summary>
        /// <returns></returns>
        private string VirtualGames()
        {
            if (_isFun)
            {
                return string.Empty;
            }

            return _appType switch
            {
                AppType.Desktop => "&game=virtualgame",
                AppType.Mobile => "&types=virtualgame",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 虛擬足球世界杯
        /// </summary>
        /// <returns></returns>
        private string VirtualSoccerWorldCup()
        {
            return _appType switch
            {
                AppType.Desktop => "&act=192",
                AppType.Mobile => "&sportid=192",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 電競
        /// </summary>
        /// <returns></returns>
        private string ESports()
        {
            var result = _appType switch
            {
                AppType.Desktop => "&game=esports",
                AppType.Mobile => "&types=esports",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(_leagueId) && _leagueId != "0")
            {
                return $"{result}&leagueid={_leagueId}";
            }

            if (!string.IsNullOrEmpty(_matchId) && _matchId != "0")
            {
                return $"{result}&matchid={_matchId}";
            }

            if (!string.IsNullOrEmpty(_betType))
            {
                return $"{result}&gameid={_betType}";
            }

            return result;
        }

        #endregion
    }
}
