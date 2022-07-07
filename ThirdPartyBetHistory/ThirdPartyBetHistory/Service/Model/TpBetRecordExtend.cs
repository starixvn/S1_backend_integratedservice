using System.Linq;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Model
{
    public class TpBetRecordExtend
    {
        public TpBetRecordExtend(TpGameTypeOption tpGameType, BetDetail betDetail)
        {
            TpId = tpGameType.TpId;
            TransId = betDetail.trans_id.ToString();
            GameType = tpGameType.Type;
            ProviderID = tpGameType.TpId.ToString();
            Provider = tpGameType.TpName;
            Product = tpGameType.TypeName;
            Category = tpGameType.CategoryId.ToString();
            Currency = betDetail.currency.ToString();

            GameID = betDetail.sport_type.ToString();
            Game = betDetail.sportname?.FirstOrDefault(i => i.lang == "en")?.name ?? string.Empty;
            RefNo = betDetail.parlay_ref_no.ToString();
            Title = betDetail.leaguename?.FirstOrDefault(i => i.lang == "en")?.name ?? string.Empty;
            MatchID = betDetail.match_id.ToString();
            BetType = betDetail.bettypename?.FirstOrDefault(i => i.lang == "en")?.name ?? string.Empty;
            BetChoice = betDetail.betchoice;
            Result = betDetail.ticket_status;
            DeviceType = GetDeviceType(betDetail.betfrom);
            IP = string.Empty;
            Odds = betDetail.odds;
            OddsTypeEnum = GetOddsType(betDetail.odds_type.Value);
            Live = betDetail.islive;
        }

        public TpBetRecordExtend(TpGameTypeOption tpGameType, BetNumberDetail betDetail)
        {
            TpId = tpGameType.TpId;
            TransId = betDetail.trans_id.ToString();
            GameType = tpGameType.Type;
            ProviderID = tpGameType.TpId.ToString();
            Provider = tpGameType.TpName;
            Product = tpGameType.TypeName;
            Category = tpGameType.CategoryId.ToString();
            Currency = betDetail.currency.ToString();

            GameID = betDetail.sport_type.ToString();
            Game = tpGameType.TypeName;
            RefNo = betDetail.trans_id.ToString();
            Title = tpGameType.TypeName;
            MatchID = betDetail.match_id.ToString();
            BetType = betDetail.bet_type.ToString();
            BetChoice = betDetail.betchoice;
            Result = betDetail.ticket_status;
            DeviceType = GetDeviceType(betDetail.betfrom);
            IP = string.Empty;
            Odds = betDetail.odds;
            OddsTypeEnum = GetOddsType(betDetail.odds_type.Value);
            Live = betDetail.islive;
        }

        public TpBetRecordExtend(TpGameTypeOption tpGameType, BetVirtualSportDetail betDetail)
        {
            TpId = tpGameType.TpId;
            TransId = betDetail.trans_id.ToString();
            GameType = tpGameType.Type;
            ProviderID = tpGameType.TpId.ToString();
            Provider = tpGameType.TpName;
            Product = tpGameType.TypeName;
            Category = tpGameType.CategoryId.ToString();
            Currency = betDetail.currency.ToString();

            GameID = betDetail.sport_type.ToString();
            Game = tpGameType.TypeName;
            RefNo = betDetail.parlay_ref_no.ToString();
            Title = tpGameType.TypeName;
            MatchID = betDetail.match_id.ToString();
            BetType = betDetail.bet_type.ToString();
            BetChoice = string.Empty;
            Result = betDetail.ticket_status;
            DeviceType = GetDeviceType(betDetail.betfrom);
            IP = string.Empty;
            Odds = betDetail.odds;
            OddsTypeEnum = GetOddsType(betDetail.odds_type.Value);
            Live = betDetail.islive;
        }

        public TpBetRecordExtend(TpGameTypeOption tpGameType, Vs3rdbetdetails betDetail)
        {
            TpId = tpGameType.TpId;
            TransId = betDetail.trans_id.ToString();
            GameType = tpGameType.Type;
            ProviderID = tpGameType.TpId.ToString();
            Provider = tpGameType.TpName;
            Product = tpGameType.TypeName;
            Category = tpGameType.CategoryId.ToString();
            Currency = betDetail.currency.ToString();

            GameID = "231";
            Game = tpGameType.TypeName;
            RefNo = betDetail.trans_id.ToString();
            Title = betDetail.BetchoiceList?.FirstOrDefault()?.Title ?? string.Empty;
            MatchID = string.Empty;
            BetType = betDetail.BetchoiceList?.FirstOrDefault()?.BetType ?? string.Empty;
            BetChoice = betDetail.BetchoiceList?.FirstOrDefault()?.BetChoice ?? string.Empty;
            Result = betDetail.ticket_status;
            DeviceType = DeviceType.None;
            IP = string.Empty;
            Odds = betDetail.odds;
            OddsTypeEnum = GetOddsType(betDetail.odds_type.Value);
            Live = string.Empty;
        }

        private DeviceType GetDeviceType(string device)
        {
            switch (device)
            {
                case "m":
                case "l":
                case "c":
                case "2":
                case "5":
                case "%":
                case "@":
                case "]":
                case "7":
                case "~":
                    return DeviceType.Mobile;
                case "1":
                case "4":
                case "[":
                case "9":
                case ">":
                    return DeviceType.Desktop;
                default:
                    return DeviceType.None;
            }
        }

        private OddsTypeEnum GetOddsType(int oddsType)
        {
            return oddsType switch
            {
                1 => OddsTypeEnum.MalayOdds,
                2 => OddsTypeEnum.ChinaOdds,
                3 => OddsTypeEnum.DecimalOdds,
                4 => OddsTypeEnum.IndoOdds,
                5 => OddsTypeEnum.AmericanOdds,
                _ => OddsTypeEnum.None,
            };
        }

        #region private Set

        public int TpId { get; private set; }
        public string TransId { get; private set; }
        public int GameType { get; private set; }

        /// <summary>
        /// 我方TpId
        /// </summary>
        public string ProviderID { get; private set; }

        /// <summary>
        /// 第三方幣別代碼 = TpCurrencyId
        /// </summary>
        public string Currency { get; private set; }

        /// <summary>
        /// 我方TpName
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// 遊戲產品 = 我方TpGameTypeName
        /// </summary>
        public string Product { get; private set; }

        /// <summary>
        /// 我方遊戲分類名稱
        /// </summary>
        public string Category { get; private set; }

        #endregion

        /// <summary>
        /// 關聯單號
        /// </summary>
        public string RefNo { get; set; } = string.Empty;

        /// <summary>
        /// 遊戲代碼 (GameCode)
        /// </summary>
        public string GameID { get; set; } = string.Empty;

        /// <summary>
        /// 遊戲名稱 (僅支援英文)
        /// </summary>
        public string Game { get; set; } = string.Empty;

        /// <summary>
        /// 遊戲標題
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 局號/桌號/賽事號/回合號
        /// </summary>
        public string MatchID { get; set; } = string.Empty;

        /// <summary>
        /// 下注類型
        /// </summary>
        public string BetType { get; set; } = string.Empty;

        /// <summary>
        /// 下注項目
        /// </summary>
        public string BetChoice { get; set; } = string.Empty;

        /// <summary>
        /// 遊戲結果
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// 下注賠率
        /// </summary>
        public decimal Odds { get; set; } = 0;

        /// <summary>
        /// 賠率類型
        /// </summary>
        public string OddsType
        {
            get
            {
                return OddsTypeEnum.ToString();
            }
        }

        /// <summary>
        /// 賠率類型
        /// </summary>
        public OddsTypeEnum OddsTypeEnum { get; set; } = OddsTypeEnum.None;

        /// <summary>
        /// 下注裝置
        /// </summary>
        public string Platform
        {
            get
            {
                return DeviceType.ToString();
            }
        }

        /// <summary>
        /// 下注裝置
        /// </summary>
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        /// <summary>
        /// 滾球
        /// </summary>
        public string Live { get; set; } = string.Empty;

        /// <summary>
        /// 玩家IP位址
        /// </summary>
        public string IP { get; set; } = string.Empty;
    }
}
