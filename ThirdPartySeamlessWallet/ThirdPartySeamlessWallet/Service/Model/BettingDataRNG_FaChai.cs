using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_FaChai
    {
        public string RecordID { get; set; }
        public string BankID { get; set; }
        public string MemberAccount { get; set; }
        public string Currency { get; set; }
        public int GameID { get; set; }
        public int GameType { get; set; }
        public bool isBuyFeature { get; set; }
        public decimal Bet { get; set; }
        public decimal Win { get; set; }
        public decimal JPBet { get; set; }
        public decimal JPPrize { get; set; }
        public decimal NetWin { get; set; }
        public decimal RequireAmt { get; set; }
        private DateTime _gameDate { get; set; }
        //FaChai傳過來的時區固定為-4轉為+0
        public DateTime GameDate
        {
            get => _gameDate;
            set => _gameDate = value.AddHours(4);
        }
        private DateTime _createDate { get; set; }
        public DateTime CreateDate 
        {
            get => _createDate;
            set => _createDate = value.AddHours(4);
        }
        public long Ts { get; set; }
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
    }

    public class BettransRng_FaChai
    {
        public long SQ { get; set; }
        public string RecordID { get; set; }
        public int TicketStatus { get; set; }
        public string Currency { get; set; }
        public decimal Bet { get; set; }
        public decimal ValidBet { get; set; }
        public decimal Win { get; set; }
        public decimal NetWin { get; set; }
        public string MemberAccount { get; set; }
        public long Ts { get; set; }
        public int GameID { get; set; }
    }
    public class CnacelData_FaChai
    {
        public string BankID { get; set; }
        public string currency { get; set; }
        public string MemberAccount { get; set; }
        public int gameID { get; set; }
        public long Ts { get; set; }
    }
    public class FCRequest
    {
        public string AgentCode { get; set; }
        public string Currency { get; set; }
        public string Params { get; set; }
        public string Sign { get; set; }
    }
    public class FCResponse
    {
        [JsonPropertyName("Result")]
        public int Result { get; set; }
        [JsonPropertyName("MainPoints")]
        public double MainPoints { get; set; }
        [JsonPropertyName("ErrorText")]
        public string ErrorText { get; set; }
    }
    public class FCWager
    {
        public decimal bet { get; set; }
        public decimal validBet { get; set; }
        public decimal prize { get; set; }
        public decimal refund { get; set; }
        public decimal winlose { get; set; }
        public decimal commission { get; set; }
        public decimal before { get; set; }
        public decimal after { get; set; }
        public decimal jptax { get; set; }
        public decimal jppoints { get; set; }
        public string recordID { get; set; }
        public string account { get; set; }
        public int gameID { get; set; }
        public int gametype { get; set; }
        public int jpmode { get; set; }
        public DateTime bdate { get; set; }
        public bool isBuyFeature { get; set; }
        public long SettleTime { get; set; }
    }
    public enum FCErrorMessage
    {
        [Description("Success")]
        Success = 0,
        [Description("Withdraw points is 0")]
        Withdraw_points_is_0 = 200,
        [Description("Abnormal points")]
        Abnormal_points = 201,
        [Description("Wrong code for deposit and withdrawal")]
        Wrong_code_for_deposit_and_withdrawal = 202,
        [Description("Your points balance not enough to withdraw")]
        Your_points_balance_not_enough_to_withdraw = 203,
        [Description("Duplicate Transaction ID number")]
        Duplicate_Transaction_ID_number = 205,
        [Description("Transaction ID number not exist")]
        Transaction_ID_number_not_exist = 221,
        [Description("Unable to obtain API customer (agent) key")]
        Unable_to_obtain_API_customer_agent_key = 301,
        [Description("Data decryption failed")]
        Data_decryption_failed = 303,
        [Description("API customer (agent) sign verification failed")]
        API_customer_agent_sign_verification_failed = 304,
        [Description("API customer (agent) sign is disabled")]
        API_customer_agent_sign_is_disabled = 401,
        [Description("This method is disabled")]
        This_method_is_disabled = 402,
        [Description("This method is under maintenance")]
        This_method_is_under_maintenance = 403,
        [Description("Game does not exist")]
        Game_does_not_exist = 405,
        [Description("Game is closed")]
        Game_is_closed = 406,
        [Description("Account locked")]
        Account_locked = 407,
        [Description("Game is under maintenance")]
        Game_is_under_maintenance = 408,
        [Description("No authorization for this account")]
        No_authorization_for_this_account = 409,
        [Description("IP not allowed")]
        IP_not_allowed = 410,
        [Description("Event not found")]
        Event_not_found = 416,
        [Description("Account does not exist")]
        Account_does_not_exist = 500,
        [Description("Account is too long")]
        Account_is_too_long = 501,
        [Description("Duplicate account")]
        Duplicate_account = 502,
        [Description("User is online")]
        User_is_online = 503,
        [Description("User is offline")]
        User_is_offline = 504,
        [Description("Account is too short")]
        Account_is_too_short = 505,
        [Description("URL is blank")]
        URL_is_blank = 601,
        [Description("API customer (agent) response is empty")]
        API_customer_agent_response_is_empty = 602,
        [Description("Return data is not in JSON format")]
        Return_data_is_not_in_JSON_format = 603,
        [Description("Verification failed")]
        Verification_failed = 604,
        [Description("No verification result is returned")]
        No_verification_result_is_returned = 605,
        [Description("Points obtain is not in numbers")]
        Points_obtain_is_not_in_numbers = 606,
        [Description("No points is returned")]
        No_points_is_returned = 607,
        [Description("Revert Cancel Bet")]
        Revert_Cancel_Bet = 799,
        [Description("Write transaction order is failed")]
        Write_transaction_order_is_failed = 702,
        [Description("Unknown errors")]
        Unknown_errors = 999,
    }
}
