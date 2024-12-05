using CommonService.Extensions;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
using CommonService.Service.BLL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace CommonService.Service.BLL.Tests
{
    
    [TestFixture]
    [TestOf(typeof(FaceBookPixelBLL))]
    public class FaceBookPixelBLLTest
    {
        private readonly FaceBookPixelBLL _faceBookPixelBll;
        
        public FaceBookPixelBLLTest()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            var configuration = app.Configuration;
            services.AddSingleton(configuration);

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };

            services.AddSingleton<IWebHostEnvironment>(env);
            services.AddSingleton<IHostEnvironment>(env);
            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient(p => Substitute.For<ILogger<HttpHelper>>());
            
            var startup = new StubStartUp(configuration);
            startup.ConfigureServices(services);
            
            var provider = services.BuildServiceProvider();
            _faceBookPixelBll = provider.GetRequiredService<FaceBookPixelBLL>();
        }
        
        [TestCase("1", ExpectedResult = ErrorCode.Success)]
        public async Task<ErrorCode> SendAsyncTest(int siteId)
        {
            var request = new PixelRequest
            {
                TransId = "completeRegistration",
                SiteId = siteId,
                CustId = 1428095,
                Value = 0,
                Url = null,
                PixelEvent = new List<FaceBookPixel>
                {
                    FaceBookPixel.CompleteRegistration
                }
            };
            
            var result = await _faceBookPixelBll.SendAsync(request);
            
            return result.ErrorCode;
        }
    
    }
}
