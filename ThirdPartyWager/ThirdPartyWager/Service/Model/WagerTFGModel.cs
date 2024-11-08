namespace ThirdPartyWager.Service.Model
{
    public class WagerTFGModel
    {
        public string id { get; set; }
        public decimal odds { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public string game_market_name { get; set; }
        public string market_option { get; set; }
        public string map_num { get; set; }
        public string bet_type_name { get; set; }
        public int event_id { get; set; }
        public DateTime? event_datetime { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? settlement_datetime { get; set; }
        public DateTime? modified_datetime { get; set; }
        public string settlement_status { get; set; }
        public string result_status { get; set; }
        public decimal? earnings { get; set; }
        public bool is_combo { get; set; }
        public string member_code { get; set; }
        public bool is_unsettled { get; set; }
        public decimal? malay_odds { get; set; }
        public decimal? euro_odds { get; set; }
        public decimal member_odds { get; set; }
        public string member_odds_style { get; set; }
        public string request_source { get; set; }
        public string tickets { get; set; }
    }

    public class TFGResult
    {
        public string id { get; set; }
        public decimal odds { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public string game_market_name { get; set; }
        public string market_option { get; set; }
        public string map_num { get; set; }
        public string bet_type_name { get; set; }
        public int event_id { get; set; }
        private DateTimeOffset _event_datetime { get; set; }
        public DateTimeOffset? event_datetime
        {
            get => _event_datetime;
            set
            {
                if (value != null)
                    _event_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
            }
        }
        private DateTimeOffset? _date_created { get; set; }
        public DateTimeOffset? date_created
        {
            get => _date_created;
            set
            {
                if (value != null)
                    _date_created = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
            }
        }
        private DateTimeOffset? _settlement_datetime { get; set; }
        public DateTimeOffset? settlement_datetime
        {
            get => _settlement_datetime;
            set
            {
                if (value != null)
                    _settlement_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
            }
        }
        private DateTimeOffset? _modified_datetime { get; set; }
        public DateTimeOffset? modified_datetime
        {
            get => _modified_datetime;
            set
            {
                if (value != null)
                    _modified_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
            }
        }
        public string settlement_status { get; set; }
        public string result_status { get; set; }
        public decimal? earnings { get; set; }
        public bool is_combo { get; set; }
        public string member_code { get; set; }
        public bool is_unsettled { get; set; }
        public decimal? malay_odds { get; set; }
        public decimal? euro_odds { get; set; }
        public decimal member_odds { get; set; }
        public string member_odds_style { get; set; }
        public string request_source { get; set; }
        public List<Ticket> tickets { get; set; }
    }

    public class Root
    {
        public int count { get; set; }
        public string next { get; set; }
        public string previous { get; set; }
        public List<TFGResult> results { get; set; }
    }

    public class Ticket
    {
        public string id { get; set; }
        public string bet_selection { get; set; }
        public decimal odds { get; set; }
        public string game_type_name { get; set; }
        public int game_type_id { get; set; }
        public string game_market_name { get; set; }
        public string bet_type_name { get; set; }
        public string competition_name { get; set; }
        public int event_id { get; set; }
        public string event_name { get; set; }
        public DateTimeOffset? event_datetime { get; set; }
        public DateTimeOffset? date_created { get; set; }
        public DateTimeOffset? settlement_datetime { get; set; }
        public string settlement_status { get; set; }
        public string result_status { get; set; }
        public string handicap { get; set; }
        public bool is_unsettled { get; set; }
        public string ticket_type { get; set; }
        public decimal malay_odds { get; set; }
        public decimal euro_odds { get; set; }
        public decimal member_odds { get; set; }
        public string member_odds_style { get; set; }
        public string request_source { get; set; }
        public string result { get; set; }
        public string market_option { get; set; }
        public string map_num { get; set; }
    }
    public class TFGUnsettleCheck
    {
        public string TransId { get; set; }
    }
    public class TFGBettrans
    {
        public string currency { get; set; }
        public string loginName { get; set; }
    }
}
