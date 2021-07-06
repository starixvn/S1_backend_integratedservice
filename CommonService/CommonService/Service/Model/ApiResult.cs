using CommonService.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.Model
{
    public class ApiResult
    {
        public ErrorCode ErrorCode { get; set; }
        public string Message { get; set; }
        public string RawErrorCode { get; set; }
        public string RawMessage { get; set; }
        public ApiResult() : this(9000, "", "", "") { }
        public ApiResult(ErrorCode error_code, string Message) : this((int)error_code, Message, "", "") { }
        public ApiResult(int ErrorCode, string Message) : this(ErrorCode, Message, "", "") { }
        public ApiResult(int _ErrorCode, string Message, string RawErrorCode, string RawMessage)
        {
            this.ErrorCode = (ErrorCode)Enum.ToObject(typeof(ErrorCode), _ErrorCode);
            this.Message = Message;
            this.RawErrorCode = RawErrorCode;
            this.RawMessage = RawMessage;
        }
    }
    public class ApiResult<TData> : ApiResult //where TData : new()
    {
        //public ApiResult() : this(9000, "", new TData()) { }
        public ApiResult() : this(9000, "", default) { }
        public ApiResult(TData data) : this(0, "", data) { }
        public ApiResult(int ErrorCode, string Message) : base(ErrorCode, Message) { }
        public ApiResult(ErrorCode ErrorCode, string Message) : base(ErrorCode, Message) { }
        public ApiResult(int ErrorCode, string Message, TData data) : base(ErrorCode, Message) { this.Data = data; }
        public ApiResult(ErrorCode ErrorCode, string Message, TData data) : base(ErrorCode, Message) { this.Data = data; }
        public TData Data { get; set; }
    }
}
