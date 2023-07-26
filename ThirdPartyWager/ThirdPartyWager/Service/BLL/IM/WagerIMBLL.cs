using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.IM;

public class WagerIMBLL
{
    private readonly ApiIMBLL _apiIMBll;
    private readonly WagerIMService _wagerIMService;
    private readonly CommonService _commonService;
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly ILogger<WagerPlayTechBLL> _logger;
    private const int TpId = (int) Tp.IM;
    private const int TimeZone = 8; // Report/GetBetLog時區為+8
    private const string TimeFormat = "yyyy-MM-dd HH.mm.ss";
    private readonly Dictionary<int, IMProductWalletEnum> _gameTypeMapper = new()
    {
        {2, IMProductWalletEnum.IMSlot},
        //{1, IMProductWalletEnum.IMSportsbook},
        //{11, IMProductWalletEnum.ESports}
    };
    
    private class IMErrorCode
    {
        public const string Success = "0";
        public const string NoData = "558";
    }
    
    private readonly Dictionary<IMProductWalletEnum, Func<IMWagerModel, int>> _pwInsertHandlerMapper;
    
    public WagerIMBLL(IServiceProvider provider)
    {
        _apiIMBll = provider.GetRequiredService<ApiIMBLL>();
        _wagerIMService = provider.GetRequiredService<WagerIMService>();
        _commonService = provider.GetRequiredService<CommonService>();
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _logger = provider.GetRequiredService<ILogger<WagerPlayTechBLL>>();
        
        _pwInsertHandlerMapper = new Dictionary<IMProductWalletEnum, Func<IMWagerModel, int>>
        {
            {IMProductWalletEnum.IMSlot, IMSoltInsertHandler},
            {IMProductWalletEnum.IMSportsbook, IMSportsInsertHandler},
            {IMProductWalletEnum.ESports, IMESportsInsertHandler}
        };
    }
    
    public async Task<bool> ExecWagerWorker(TpListModel siteData, int type, int timeRange)
    {
        var siteId = siteData.SiteId;
        var quick = false;
        const int excessMin = 120;
        const int varySec = 1;
        
        foreach (var (gameType, productWallet) in _gameTypeMapper)
        {
            var workerTime = GetWorkerTime(type, varySec, siteId, gameType);
            quick = CheckQuick(workerTime, excessMin);
            
            var time = _dateTimeHelper.GetTimeByMinute(timeRange - 1, TimeZone, TimeFormat, workerTime);
            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(TimeZone, 0, 0)).ToUnixTimeSeconds();

            if (time.endUnix >= cvTime)
                continue;
            
            var result = await GetWagersAsync(time, siteData, productWallet);
            if (result)
                _thirdPartyBll.SetThirdPartyTimerByReference(TpId, type, time.endUnix, gameType, siteId.ToString());
        }
        if (quick)
            Console.WriteLine("IS QUICK");
        return quick;
    }

    #region Private
    
    private async Task<bool> GetWagersAsync(TimeCast wagerTime, TpListModel tpInfo, IMProductWalletEnum productWallet)
    {
        const string workerName = "IM_GetWagers";
        string msg;
        try
        {
            var tpCurrencyList = _commonService.GetTpCurrencyList(TpId);
            var siteId = tpInfo.SiteId;
            
            foreach (var tpCurrency in tpCurrencyList)
            {
                var param = new GetBetListByTimeParam
                {
                    Setting = tpInfo.SettingDic,
                    Currency = tpCurrency.TpCurrencyId,
                    ProductWallet = productWallet,
                    StartTime = wagerTime.startTime,
                    EndTime = wagerTime.endTime
                };

                var success = await DoGetWagers(param);

                if (!success)
                    return false;
            }
            
            msg = "FINISH";
            _logger.LogInformation($"{workerName} {msg}\r\nsiteId={siteId}, ProductWallet={productWallet}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");
            return true;
        }
        catch (Exception ex)
        {
            msg = "FATAL_ERROR";
            _logger.LogError(ex, $"{workerName} {msg}\r\nsiteId={tpInfo.SiteId}, ProductWallet={productWallet}\r\n{ex.Message}");
            return false;
        }
    }

    private int IMSoltInsertHandler(IMWagerModel resp)
    {
        var data = ObjToClass<IEnumerable<IMSlotModel>>(resp.Result);
        var wagers = data.Select(x => new IMSlotWager(x));
        
        return _wagerIMService.InsertSlotWager(wagers);
    }
    
    private int IMSportsInsertHandler(IMWagerModel resp)
    {
        var data = ObjToClass<IEnumerable<IMSportsbookModel>>(resp.Result);
        var wagers = data.Select(x => new IMSportsWager(x));
        
        return _wagerIMService.InsertSportsWager(wagers);
    }
    
    private int IMESportsInsertHandler(IMWagerModel resp)
    {
        var data = ObjToClass<IEnumerable<IMESportsModel>>(resp.Result);
        var wagers = data.Select(x => new IMESportsWager(x));
        
        return _wagerIMService.InsertESportsWager(wagers);
    }

    private T ObjToClass<T>(object obj)
        => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));

    private async Task<bool> DoGetWagers(GetBetListByTimeParam param)
    {
        var workerName = param.ProductWallet + "_GetWagers";
        var page = 1;
        var totalPage = 1;
        while (page <= totalPage)
        {
            param.Page = page;
            var resp = await _apiIMBll.GetBetListByTime(param);

            if (resp.Code == IMErrorCode.NoData)
            {
                _logger.LogInformation($"{workerName} NO_DATA");
                await Task.Delay(10000); // 1分鐘內發送多筆會被收到併發錯誤，稍停
                return true;
            }
            
            if (resp.Code != IMErrorCode.Success)
            {
                _logger.LogError($"{workerName} THIRD_PARTY_ERROR: Code={resp.Code}, Message={resp.Message}");
                return false;
            }
            
            totalPage = resp.Pagination.TotalPage;

            try
            {
                var result = _pwInsertHandlerMapper[param.ProductWallet].Invoke(resp);
                var count = resp.Result.Count();
                if (result != count)
                {
                    _logger.LogInformation($"{workerName} INSERT_WAGER COUNT_NOT_MATCH: affected={result}, data count={count}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{workerName} INSERT_WAGER FAILED: {ex.Message}");
                return false;
            }
            
            page++;
        }

        return true;
    }
    
    private DateTime GetWorkerTime(int type, int varySec, int siteId, int gameType)
    {
        var lastTime =
            _thirdPartyBll.GetThirdPartyTimerReferenceByReference(TpId, type, gameType, siteId.ToString());
        
        return lastTime == null
            ? DateTime.Now.AddHours(-8)
            : lastTime.StartTime.AddSeconds(varySec);
    }
    
    
    private static bool CheckQuick(DateTime workTime, int excess)
    {
        var diff = DateTime.Now - workTime;
        return DateTime.Now > workTime && diff.TotalMinutes > excess;
    }

    #endregion
}