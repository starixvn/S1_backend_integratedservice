namespace ThirdPartySeamlessWallet.Service.Model
{

    public class ApiResult<TData>
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public string RawErrorCode { get; set; }
        public string RawMessage { get; set; }
        public TData Data { get; set; }
    }
}
