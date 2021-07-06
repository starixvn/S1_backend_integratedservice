using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        public ApiResult SendMessage(FireBaseMessage messageItem)
        {
            var apiResult = new ApiResult();
            try
            {
                var config = fireBaseConfig.Value[messageItem.SiteName];
                if (config == null)
                {
                    apiResult.ErrorCode = ErrorCode.SettingNotFound;
                    apiResult.Message = "Config error";
                    return apiResult;
                }

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
                bool isTopicsMessage = messageItem.Token.Contains("topics");
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

                                var Response = JsonConvert.DeserializeObject<FireBaseResponse>(sResponseFromServer);
                                if (((HttpWebResponse)tResponse).StatusCode == HttpStatusCode.OK)
                                {
                                    if (Response.success > 0 || isTopicsMessage)
                                        apiResult.ErrorCode = ErrorCode.Success;
                                    else
                                        apiResult.ErrorCode = ErrorCode.Fail;
                                }
                                else
                                    apiResult.ErrorCode = ErrorCode.Fail;
                                apiResult.Message = sResponseFromServer;

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = ErrorCode.OTHER_ERROR;
                apiResult.Message = ex.Message;

            }



            return apiResult;
        }
        public async Task<ApiResult> SendMessageAsync(FireBaseMessage messageItem)
        {
            var apiResult = new ApiResult();
           
            try
            {
                var config = fireBaseConfig.Value[messageItem.SiteName];
                if (config == null)
                {
                    apiResult.ErrorCode = ErrorCode.SettingNotFound;
                    apiResult.Message = "Config error";
                    return apiResult;
                }
                //伺服器
                string applicationID = config.ApplicationID;

                string senderId = config.SenderId;

                string deviceId = messageItem.Token;
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("https://fcm.googleapis.com/fcm/");
                client.DefaultRequestHeaders
                                      .Accept
                                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("key", "=" + applicationID);
                client.DefaultRequestHeaders.Add("Sender", "id=" + senderId);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "relativeAddress");
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
                request.Content = new StringContent(json,
                                                    Encoding.UTF8,
                                                            "application/json");//CONTENT-TYPE header

                var data1 = await client.PostAsync("send", request.Content);
                bool isTopicsMessage = messageItem.Token.Contains("topics");
                var Response = JsonConvert.DeserializeObject<FireBaseResponse>(data1.Content.ReadAsStringAsync().Result);
                if (data1.IsSuccessStatusCode)
                {
                    if (Response.success > 0 || isTopicsMessage)
                        apiResult.ErrorCode = ErrorCode.Success;
                    else
                        apiResult.ErrorCode = ErrorCode.Fail;
                }
                else
                    apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = data1.Content.ReadAsStringAsync().Result;
                return apiResult;

            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = ErrorCode.OTHER_ERROR;
                apiResult.Message = ex.Message;

            }



            return apiResult;
        }
    }
}
