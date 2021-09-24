using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface ITpBetRecordProvider
    {
        TpBetRecordDetailModel GetTpBetRecordDetail(GetBetDetailRequest request);
    }
}
