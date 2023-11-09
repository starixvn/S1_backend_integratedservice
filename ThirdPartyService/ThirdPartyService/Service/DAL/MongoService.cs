using MongoDB.Driver;
using ThirdPartyCore.BLL;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL
{
    public interface ITpAuthTokenService
    {
        void InsertTpAuthToken(TpAuthToken document);
    }

    public class TpAuthTokenService : ITpAuthTokenService
    {
        private readonly IMongoCollection<TpAuthToken> _collection;

        public TpAuthTokenService(IMongoClient mongoClient, ConfigBLL configBll)
        {
            var dbSetting = configBll.GetConfigValue<TpAuthDbSetting>("TpAuthDBWithCollection");
            var database = mongoClient.GetDatabase(dbSetting.DbName);
            _collection = database.GetCollection<TpAuthToken>(dbSetting.CollectionName);
        }

        public void InsertTpAuthToken(TpAuthToken document)
        {
            _collection.InsertOne(document);
        }
    }
}
