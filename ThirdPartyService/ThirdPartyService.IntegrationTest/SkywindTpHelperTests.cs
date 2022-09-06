using NUnit.Framework;
using System.Collections.Generic;
using ThirdPartyCore.Model;
using FluentAssertions;
using ThirdPartyCore.Model.enums;
using Newtonsoft.Json;
using System.Web;
using System.Diagnostics;
using System;
using ThirdParty.Skywind;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using ThirdPartyService.Extensions;
using ThirdPartyService.IntegrationTest.Models;
using Microsoft.Extensions.Hosting;
using ThirdPartyCore;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using ThirdPartyService.IntegrationTest.Stubs;
using Microsoft.Extensions.Logging;
using ThirdPartyService.Service.Library;
using Microsoft.Extensions.Configuration;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;

namespace ThirdParty.Skywind.Tests
{
    [TestFixture()]
    public class SkywindTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private Dictionary<string, string> _inputs;
        private IConfiguration _configuration;

        public SkywindTpHelperTests()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });
            builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());
            var app = builder.Build();
            _configuration = app.Configuration;
            services.AddSingleton(_configuration);

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };
            ThirdPartyHelperBase.IsDevelopment = env.IsDevelopment();
          

            services.AddSingleton<IWebHostEnvironment>(env);
            services.AddSingleton<IHostEnvironment>(env);

            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient(p => Substitute.For<ILogger<ThirdPartyBLL>>());
            services.AddTransient(p => Substitute.For<ILogger<ThirdPartyHelper>>());
            services.AddTransient(p => Substitute.For<ILogger<Log2File>>());

            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();
            ThirdPartyHelperBase.AuthProvider = provider.GetRequiredService<AuthBLL>();
            ThirdPartyHelperBase.TpCustInfoProvider = provider.GetRequiredService<TpCustInfoBLL>();
        }


        [SetUp]
        public void Setup()
        {
            //Arrange
            _tpHelper = new TpHelper();

            //_tpCustInfo = new TpCustInfo()
            //{
            //    TpId = 33,
            //    CustId = 249951,
            //    SiteId = 1,
            //    UserName = "hanstest01",
            //    TpUserName = "xnova88x249951",
            //    TpPwd = "h5N3Bjn",
            //    TpCurrencyId = "MYR",
            //    ExchangeRate = 1.0000m,
            //    IsTest = false,
            //    ActStatus = 2,
            //    Status = 1,
            //    CStatus = 1,
            //};

            _tpCustInfo = new TpCustInfo()
            {
                TpId = 43,
                CustId = 250072,
                SiteId = 1,
                UserName = "willie1016",
                TpUserName = "DNova88_250072",
                TpPwd = "QQ77889",
                TpCurrencyId = "MYR",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            //_inputs = new Dictionary<string, string>
            //{
            //    { "GameUrl_MYR", "https://tcgmyr.nova88asia.com/" },
            //    { "ApiUrl", "http://www.connect6play.com/doBusiness.do" },
            //    { "HomeUrl", "https://www.nova88.net/" },
            //    { "MerchantCode_MYR", "xpnovamyr" },
            //    { "DESKey_MYR", "dHJoyySB" },
            //    { "SHA256Key_MYR", "70tOVKomTbE5booC" },
            //};

            _inputs = new Dictionary<string, string>
            {
                { "ApiUrl", "https://api.gcpstg.m27613.com/v1" },
                { "SecretKey", "375186ce-7110-4370-84ae-0bd6fc833762" },
                { "Username", "Nova88_merchant_api_stg" },
                { "Password", "STwgnTj3jNxpTKZH" },
                { "gameId", "sw_gatc" },
                //{ "gameId", "0" },
                { "MerchId", "sw_Nova88_merchant_stg" },
                { "MerchPwd", "tzlbpqhXqgmim8qm" },
                { "Token", "lQqFbMJm0UAsVBAWmINsA"},
                { "StagingLobbies", "https://2bgz0erb-lobby.ss211208.com"}
            };
        }

        [Test()]
        public void CreateMemberTest()
        {
            //Act
            var actual = _tpHelper.CreateMember(_tpCustInfo, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test()]
        public void LoginTest()
        {
            //Arrange
            var appType = AppType.Desktop;
            var tpLangId = "ko";

            //Act
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        //[Test()]
        //public void DesEncryptTest()
        //{
        //    //Arrange
        //    //var sParam = new
        //    //{
        //    //    method = "cm",
        //    //    username = _tpCustInfo.TpUserName,
        //    //    password = _tpCustInfo.TpPwd,
        //    //    currency = _tpCustInfo.TpCurrencyId
        //    //};

        //    var sParam = new
        //    {
        //        method = "sgb",
        //        username = _tpCustInfo.TpUserName,
        //        product_type = "384",//420,
        //        request_time = DateTime.Now.ToFileTime(),
        //    };

        //    var merchantCode = _inputs["MerchantCode_MYR"];
        //    var desKey = _inputs["DESKey_MYR"];
        //    var SignKey = _inputs["SHA256Key_MYR"];
        //    var hashString = ToJson(sParam);

        //    //Act
        //    var desEncrypt = _tpHelper.DesEncrypt(hashString, desKey);
        //    var sign = _tpHelper.SHA256($"{desEncrypt}{SignKey}");

        //    var requestBody = $"?merchant_code={HttpUtility.UrlEncode(merchantCode)}&params={HttpUtility.UrlEncode(desEncrypt)}&sign={HttpUtility.UrlEncode(sign)}";

        //    //Assert
        //    var result = _tpHelper.DesDecrypt(desEncrypt, desKey);
        //    result.Should().Be(hashString);


        //    var resultdesEncrypt = _tpHelper.DesEncrypt(result, desKey);
        //    var checkSing = _tpHelper.SHA256($"{resultdesEncrypt}{SignKey}");
        //    sign.Should().Be(checkSing);
        //}


        private string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}
