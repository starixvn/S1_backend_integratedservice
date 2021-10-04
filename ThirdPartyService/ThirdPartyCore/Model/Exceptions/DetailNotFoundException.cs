using System;


namespace ThirdPartyCore.Model.Exceptions
{
    public class DetailNotFoundException : Exception
    {
        public DetailNotFoundException()
        {
        }
        public DetailNotFoundException(string message) : base(message)
        {
        }
    }
}
