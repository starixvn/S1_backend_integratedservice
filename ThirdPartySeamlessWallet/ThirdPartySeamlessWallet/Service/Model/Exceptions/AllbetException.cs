using System;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class AllbetException : Exception
    {
        public int resultCode { get; set; }
        public string message { get; set; }

        public AllbetResultCode ReturnCode { get; set; }

        public AllbetException(AllbetResultCode returnCode, string msg) : base(msg)
        {
            resultCode = (int)returnCode;
            message = msg;
            ReturnCode = returnCode;
        }

        public AllbetException(AllbetResultCode returnCode)
        {
            resultCode = (int)returnCode;
            message = returnCode.ToString();
            ReturnCode = returnCode;
        }
    }
}