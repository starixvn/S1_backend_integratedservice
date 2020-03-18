using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartyCore
{
    public interface IAuthProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="Expiry"></param>
        bool ApplyToken(string Key, string Value, int Expiry = 1);
        string Authenticate(string Key);
    }
}
