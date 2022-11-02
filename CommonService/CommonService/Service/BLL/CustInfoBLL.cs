using CommonService.Service.DAL;
using CommonService.Service.Model;

namespace CommonService.Service.BLL
{
    public class CustInfoBLL
    {
        private readonly CustInfoService custInfoService;

        public CustInfoBLL(CustInfoService custInfoService)
        {
            this.custInfoService = custInfoService;
        }

        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            return custInfoService.GetCustInfoProfile(custId);
        }

    }
}
