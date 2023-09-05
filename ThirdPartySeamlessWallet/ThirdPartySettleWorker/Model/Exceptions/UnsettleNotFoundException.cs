namespace ThirdPartySettleWorker.Model.Exceptions
{
    public class UnsettleNotFoundException : Exception
    {
        public UnsettleNotFoundException()
        {
        }
        public UnsettleNotFoundException(string message) : base(message)
        {
        }
    }
}
