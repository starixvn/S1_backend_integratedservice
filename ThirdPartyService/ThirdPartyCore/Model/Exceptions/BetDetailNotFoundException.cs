using System;


namespace ThirdPartyCore.Model.Exceptions
{
    public class BetDetailNotFoundException : Exception
    {
        public BetDetailNotFoundException()
        {
        }
        public BetDetailNotFoundException(string message) : base(message)
        {
        }
    }
}
