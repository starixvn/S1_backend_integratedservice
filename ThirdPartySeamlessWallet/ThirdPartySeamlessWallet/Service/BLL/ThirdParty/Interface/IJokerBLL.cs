using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IJokerBLL
    {
        Task<JokerResponse> JoinTournament(JoinAndWinRequest request);
        Task<JokerResponse> WinTournament(JoinAndWinRequest request);
        Task<JokerResponse> CancelTournament(CancelRequest request);
    }
}
