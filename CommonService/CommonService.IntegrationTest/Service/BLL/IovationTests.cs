using System.Security.Cryptography.X509Certificates;
using System.Text;
using CommonService.Extensions;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
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
    [TestFixture()]
    public class IovationBLLTests
    {
        private IConfiguration _configuration;
        private readonly IovationBLL _iovationBLL;
        private readonly IHostEnvironment _env;
        private readonly string _storagePath;

        public IovationBLLTests()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "LOCAL",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            _configuration = app.Configuration;
            services.AddSingleton(_configuration);

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };

            services.AddSingleton<IWebHostEnvironment>(env);
            services.AddSingleton<IHostEnvironment>(env);

            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient(p => Substitute.For<ILogger<HttpHelper>>());
            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _iovationBLL = provider.GetRequiredService<IovationBLL>();
            _env = provider.GetRequiredService<IHostEnvironment>();
            _storagePath = $@"{_env.ContentRootPath}Storage\Iovation";
        }

        [TestCase("Nova88", ExpectedResult = ErrorCode.Success)]
        [TestCase("Goal123", ExpectedResult = ErrorCode.Success)]
        [TestCase("Bwing", ExpectedResult = ErrorCode.Success)]
        [Ignore("要測試再註解掉，")]
        public async Task<ErrorCode> IovationChecksAsyncTest(string siteName)
        {
            var iovationInfo = new IovationChecksRequest()
            {
                accountCode = 650037,
                blackbox = "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;0400lY+TLEkg2MiVebKatfMjIHvCR1Q0hjCy5FG9BCvPvGzbehwuEoAH6t9867L037WJnO/OsRrCVUwFgTAsqir5pJRvFCMSbl5xwDKZLh2kODdrI1tkc2hicypq4/eQIAw4wKr0Mf+ogWbuXvP/g+9owl/nBVbokq6ySUjkiQPGwF8grBH5xvykmUPKtcSzTI8WFMIL+5rl50RJNVzOUsXjtZ8/QylK5rlnljI5R8kpPMt5QqtpmG61Bh2+3pwnaIUAN+8GgPlTn8PanBrTdkKMH9ocffTgFFudTNPikOfLK71sDVW0AC6h7IzYv0QIGm5NWSYCumPWFPfJUog+119FGOb7wVkL6OOnWaawn/RHSLeZZCMuWSJstzMkfBVSkAve99V6MaAfuu4WHWS9Vn7JAxjCWqV+LOuGdMv62aH7T1e5zSAJQbf+FdufUq1PP0xIVmiy9cSVKqQrl8RBjt7aRrNCb7eqEP2SncdnkwVO0nbOIcDe3iuKI6Si+qSj99Kalv/nuXQXQGutzRNySHv93tJy1Hqg++hz8cFCb3XpK0LXm0XBtvk2RW2s0fO33l4+Bez6hBBzrS2usa2jjh0Vn+TQHjy7H5XhLHCHAGY3J0jpLqVrSVzsI1LB6M5fl2EoMEAoEitEttFUo1DRkUtPGM4SBG63TzcTkTvlqBdC12PmY1XpDGBtyUYBKknoROYtu07EpiQEVc3MPwZ9IQ/tCz/KhUgGeNqKwHaf9f8MJCd+/u06EOo/UEYn9EFMRfvxVguGdCj5lhfj7XeFpgP90tfidlHjZD64OENgY4kVU9wB84+POrI4MkoD4iHJ5a1Qmz61zhzFB1cmuxfCytyYYcjwjs5Ca2/wBdhim9vK/UsQ5XOYm0OqKbAtjgzHMGAkLTLkEN4L7EfI8I7OQmtv8AXYYpvbyv1LEOVzmJtDqimwLY4MxzBgJC0y5BDeC+xHyPCOzkJrb/AF2GKb28r9SxfwKMcRIoeJtvb3n2nbY+KorI7cN/HIKJ8UIS8xP1xzpdqSgJrtcJbn4KW2br+bRU8xAXTS4l7DQtPYEf0PHxk1gG7+dJK6MAX8iYyy5jvSS4e4f0GE7S3tsTaim82JoKuB5xKjTqLNXOp4yYEIf+L1yYqstDFzEDk4TYqQ5l7/SNfOTvsgtn95SxDtE2suujWRMoizXj3piSkiHcDvTuWbZ7blJBujUxfC7+Qeu2t1BiwzOH2ZEvh4VShbY+EVBDdML6ZhV5yEfobiOhcONJPNnBg89k56rFuhOU+0o207fZ68PqrXE3b2Gbup08voryJ0sNRD5FoMoIVsCzq/y58cLyxCAVOWJXxIhRiBgIS7LGydrz1ihh9xNpJ7orOpeMhAbUcp/MmeI0SxW/vZYWEdsGPBdAE5dGXx8nAQ/2BKLKU3hT1O6dqMvd12/e9FYmkl+ZRPL0/wOZxZ1PiTPXIB0N1CvMfPZiPArmS8uxtlwAE9tSNPFXqdKQOfvCJ9c7cygaEbx5zn+lp4GCcdMkrcMcHeGovSxbmss1DBBqDtAaIwK99XjZT0nVUtNwHEh0Ufvm9KNXJ57tjCQkotdjQw5xWQZwLvBGimErzBAEkGLAlDzAB+HIfAGxt1Z9LIxG//5vtKbmAhdbsLkKbAYkcjy29+OKQTNdgnN/Fkv/Rft2jHva0qZ+PRz/EyFSooVbrmnmesPGh9LKhMz6ShTZ8cM/lsneyYBivbuAQnKlVTngMpK9iwbkUt5/lqrQZ45a07RSeLq6w7wLbfRTFc49Oa8gcn3LoQ2OZEOokqhSQgWSwSiF/9rnArAr7K5W+RP/NQ4Az9/9YdjU4xpFRX74aQy3hQrQVnGMGisaPaMo30nj+J3x/etye1DnJ05CS8yva9Sqqg7md9snxbl+AZpbEgR1wIjQHqiQ==",
                statedIp = "218.32.244.12",
                transactionInsight = new Dictionary<string, string>
                {
                    { "onlineId", "ricktestvndsaba" },
                    { "billingCountry", "VN" },
                    { "valueCurrency", "VND" }
                },
                type = "withdrawal"
            };

            var actual = await _iovationBLL.IovationChecksAsync(iovationInfo);
            return actual.ErrorCode;
        }
    }
}

