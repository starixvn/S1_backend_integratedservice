namespace ThirdPartyWager.Service.Model
{
    public class WagerGAModel
    {
    }

    public class GATickets 
    {
        public string sid { get; set; }
        public DateTime created_date_time { get; set; }
        public DateTime modified_date_time { get; set; }
        public DateTime? settled_date_time { get; set; }
        public string arena_code { get; set; }
        public string match_day_code { get; set; }
        public int fight_number { get; set; }
        public string selection { get; set; }
        public decimal odds { get; set; }
        public decimal bet_amount { get; set; }
        public decimal ticket_amount { get; set; }
        public decimal ante_amount { get; set; }
        public int status { get; set; }
        public string result { get; set; }
        public string member_ref_id { get; set; }
        public string account_id { get; set; }
        public decimal win_loss_amount { get; set; }
        public string currency_code { get; set; }
    }
}
