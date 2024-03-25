using CommonService.Service.DAL;
using CommonService.Service.Model;

namespace CommonService.Service.BLL
{
    public class CustInfoBLL
    {
        private readonly CustInfoService _custInfoService;

        public CustInfoBLL(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
        }

        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            return _custInfoService.GetCustInfoProfile(custId);
        }

    }
}
