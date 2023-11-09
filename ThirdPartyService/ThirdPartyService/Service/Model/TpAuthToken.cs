using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ThirdPartyService.Service.Model
{
    public class TpAuthToken
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Token { get;set; }

        public string Value { get; set; }

        public int Expiry { get; set; }

        public TpAuthToken(string token, string value, int expiry)
        {
            Token = token;
            Value = value;
            Expiry = expiry;
        }
    }
}
