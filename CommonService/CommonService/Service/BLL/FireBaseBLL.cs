using CommonService.Service.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class FireBaseBLL
    {
        private readonly IOptions<Dictionary<string, FireBaseConfig>> fireBaseConfig;

        public FireBaseBLL(IOptions<Dictionary<string, FireBaseConfig>> fireBaseConfig)
        {
            this.fireBaseConfig = fireBaseConfig;
        }
        public bool SendMessage(FireBaseMessage messageItem, out String ?returnString)
        {
            returnString = " ";
            try
            {
                var config = fireBaseConfig.Value[messageItem.SiteName];
                if (config == null)
                    return false;

                //伺服器
                string applicationID = config.ApplicationID;

                string senderId = config.SenderId;

                string deviceId = messageItem.Token;
                //string deviceId = "/topics/Test";


                WebRequest tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
                tRequest.Method = "post";
                tRequest.ContentType = "application/json";
                var data = new
                {
                    to = deviceId,
                    notification = new
                    {
                        body = messageItem.Body,
                        title = messageItem.Title

                    }
                };

                var json = JsonConvert.SerializeObject(data);
                Byte[] byteArray = Encoding.UTF8.GetBytes(json);
                tRequest.Headers.Add(string.Format("Authorization: key={0}", applicationID));
                tRequest.Headers.Add(string.Format("Sender: id={0}", senderId));
                tRequest.ContentLength = byteArray.Length;
                using (Stream dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    using (WebResponse tResponse = tRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                String sResponseFromServer = tReader.ReadToEnd();
                                returnString = sResponseFromServer;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message;

            }



            return false;
        }
    }
}
