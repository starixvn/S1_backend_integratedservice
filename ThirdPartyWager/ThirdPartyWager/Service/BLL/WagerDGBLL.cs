using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{

    public class WagerDGBLL
    {
        public IEnumerable Result { get; private set; }

        private readonly IHttpClientFactory _clientFactory;

        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly WagerDGService wagerDGService;
        private readonly ApiDGBLL apiDGBLL;

        public WagerDGBLL(IHttpClientFactory clientFactory, ThirdPartyHelper thirdPartyHelper, WagerDGService wagerDGService, ApiDGBLL apiDGBLL)
        {
            _clientFactory = clientFactory;
            this.thirdPartyHelper = thirdPartyHelper;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
        }

        public async Task GetWagersAsync()
        {
            // get dg api info
            int tpId = 4;
            var dataTpList = thirdPartyHelper.GetThirdPartySetting(tpId);
            var setting = JsonConvert.DeserializeObject<Setting>(dataTpList.Setting);

            // get wagers
            var dataWager = await apiDGBLL.GetBetList(setting.ApiUrl, setting.AgentAccount, setting.APIkey);
            // insert to db
            var count = wagerDGService.InsertWagerDG(dataWager.list);
            // get all wager ids
            List<long> wagerIds = new List<long>();

            foreach(var item in dataWager.list)
            {
                wagerIds.Add(item.id);
            }

            // set wager as mark
            await apiDGBLL.MarkGotBetList(setting.ApiUrl, setting.AgentAccount, setting.APIkey, wagerIds);


            //string domain = setting.ApiUrl;
            //string uri = "/game/getReport/";
            //string agentName = setting.AgentAccount;
            //string apiKey = setting.APIkey;
            //string random = GetRandomString(8);
            //string apiUrl = $"{domain}{uri}{agentName}";
            //string token = ToMD5($"{agentName}{apiKey}{random}").ToLower();

            //// call api
            //var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            //var postData = new { token = $"{token}", random = $"{random}" };
            //var json = JsonConvert.SerializeObject(postData);
            //request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            //var client = _clientFactory.CreateClient();
            //// response
            //var response = await client.SendAsync(request);
            //if (response.IsSuccessStatusCode)
            //{
            //    string r = response.Content.ReadAsStringAsync().Result;
            //    var dataWager = JsonConvert.DeserializeObject<WagerDGModel>(r);

            //    if (dataWager.codeId != 0)
            //    {
            //        // return errorcode
            //    }
            //    if (dataWager.list == null || dataWager.list.Count() == 0)
            //    {
            //        // no data
            //        Console.WriteLine("list null");
            //        // return errorcode
            //    }
            //    Console.WriteLine("in");
            //    // write bet record
            //    var count = wagerDGService.InsertWagerDG(dataWager.list);
            //    if(count > 0)
            //    {
            //        // mark ids
            //    }

            //}
            //else
            //{
            //    // errorcode
            //    Result = response.StatusCode.ToString();
            //}
            //// return errorcode
            //return null;
            ////return (IEnumerable<dynamic>)Result;
        }


        //protected string GetRandomString(int length)
        //{
        //    var str = "0123456789abcdefghijklmnopqrstuvwxyz";//ABCDEFGHIJKLMNOPQRSTUVWXYZ
        //    var next = new Random();
        //    var builder = new StringBuilder();
        //    for (var i = 0; i < length; i++)
        //    {
        //        builder.Append(str[next.Next(0, str.Length)]);
        //    }
        //    return builder.ToString();
        //}

        //protected string ToMD5(string encodeStr)
        //{
        //    using var cryptoMD5 = MD5.Create();
        //    var bytes = Encoding.UTF8.GetBytes(encodeStr);
        //    var hash = cryptoMD5.ComputeHash(bytes);
        //    var md5 = BitConverter.ToString(hash)
        //      .Replace("-", string.Empty);

        //    return md5;
        //}

    }
}
