using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IJokerBLL
    {
        Task<JokerResponse> JoinTournament(Dictionary<string, string> request);
        Task<JokerResponse> WinTournament(Dictionary<string, string> request);
        Task<JokerResponse> CancelJoinTournament(Dictionary<string, string> request);
    }
}
