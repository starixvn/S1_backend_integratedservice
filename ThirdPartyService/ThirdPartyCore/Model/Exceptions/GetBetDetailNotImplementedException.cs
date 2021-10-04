using System;

namespace ThirdPartyCore.Model.Exceptions
{

    public class GetBetDetailNotImplementedException : Exception
    {
        public GetBetDetailNotImplementedException()
        {
        }
        public GetBetDetailNotImplementedException(string message) : base(message)
        {
        }
    }
}
