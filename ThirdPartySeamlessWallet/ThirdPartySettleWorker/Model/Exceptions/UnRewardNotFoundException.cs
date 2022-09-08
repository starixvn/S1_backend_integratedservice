using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartySettleWorker.Model.Exceptions
{
    public class UnRewardNotFoundException : Exception
    {
        public UnRewardNotFoundException()
        {
        }
        public UnRewardNotFoundException(string message) : base(message)
        {
        }
    }
}
