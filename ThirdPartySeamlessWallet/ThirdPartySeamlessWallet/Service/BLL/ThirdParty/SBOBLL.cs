using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class SBOBLL : ISBOBLL
    {
        public Tp ThirdParty { get; set; }
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<SBOBLL> _logger;

        private Dictionary<SBOProductType, GameType> _sboGameTypeMap = new Dictionary<SBOProductType, GameType>()
        {
            { SBOProductType.SportsBook, GameType.SBO_Sports }
        };

        public SBOBLL(ThirdPartyBLL thirdPartyBll, TpCustInfoBLL tpCustInfoBll, ILogger<SBOBLL> logger)
        {
            ThirdParty = Tp.SBO;
            _thirdPartyBLL = thirdPartyBll;
            _tpCustInfoBLL = tpCustInfoBll;
            _logger = logger;
        }

        public SBOBalanceResponse GetBalance(SBOBalanceRequest request)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.Username);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new SBOException(SBOErrorCode.MemberNotExist, $"{request.Username} is locked");
            }
            
            var gameType = _sboGameTypeMap[request.ProductType];
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, request.Username, (int)gameType);
            if (tpCustBalance != null)
            {
                return new SBOBalanceResponse()
                {
                    AccountName = request.Username,
                    Balance = tpCustBalance.Balance,
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                };
            }
            throw new SBOException(SBOErrorCode.NotEnoughBalance,
                $"{ThirdParty} {gameType}: {request.Username} get balance failed");
        }
    }
}