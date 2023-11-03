namespace ThirdPartyWager.Service.Model
{
    public class WagerBNGModel
    {
    }
    public class TransactionData
    {
        public string fetch_state { get; set; }
        public List<item> items { get; set; }
    }
    public class item
    {
        public string transaction_id { get; set; }
        public string original_transaction_id { get; set; }
        public string type { get; set; }
        public string player_id { get; set; }
        public string brand { get; set; }
        public string currency { get; set; }
        public string mode { get; set; }
        public bool is_test { get; set; }
        public string platform { get; set; }
        public string tag { get; set; }
        public long round_id { get; set; }
        public bool round_started { get; set; }
        public bool round_finished { get; set; }
        public string bet { get; set; }
        public string win { get; set; }
        public string balance_after { get; set; }
        public bool auto_paid { get; set; }
        public string provider_id { get; set; }
        public string provider_name { get; set; }
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string status { get; set; }
        public bool manual_confirmed { get; set; }
        public DateTimeOffset c_at { get; set; }
        public int? bonus_id { get; set; } = 0;
        public string campaign { get; set; }
        public string bonus_source { get; set; }
        public string ext_bonus_id { get; set; }
        public string bonus_event { get; set; }
        public string bonus_type { get; set; }
    }
}
