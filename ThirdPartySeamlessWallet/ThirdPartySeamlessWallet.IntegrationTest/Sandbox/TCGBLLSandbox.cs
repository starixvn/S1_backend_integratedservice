using System.Data;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using NSubstitute;

using ThirdPartySeamlessWallet.Core.Extensions;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.Stubs;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ThirdPartySeamlessWallet.IntegrationTest.SandBox
{
    public class TCGBLLSandbox
    {
        private IServiceProvider _serviceProvider;

        private readonly ServiceCollection _serviceCollection;

        private IConfiguration _configuration;

        private readonly MySqlClient _mySqlClient;

        private readonly string _merchantCode;

        private readonly string _tpUserName;

        private Dictionary<TCGTransType, Lazy<string>> _exceptSqlMapping;

        private readonly TCGBLL _target;

        public TCGReturnObj Result;

        public TCGBalanceObj Balance;

        public TCGCheckObj Check;

        private Dictionary<string, string> _inputs;

        public TCGBLLSandbox(Dictionary<string, string> inputs)
        {
            _inputs = inputs;
            _merchantCode = inputs["MerchantCode"];
            _tpUserName = inputs["TpUserName"];

            _serviceCollection = new ServiceCollection();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            _configuration = app.Configuration;
            _serviceCollection.AddSingleton(_configuration);

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };
            _serviceCollection.AddSingleton<IWebHostEnvironment>(env);
            _serviceCollection.AddSingleton<IHostEnvironment>(env);

            _serviceCollection.AddTransient(p => Substitute.For<ILogger<SettledService>>());
            _serviceCollection.AddTransient(p => Substitute.For<ILogger<BettingService>>());

            var startUp = new StubStartUp(_configuration);
            startUp.ConfigureServices(_serviceCollection);
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _mySqlClient = _serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

            _target = new TCGBLL(_serviceProvider);

            _exceptSqlMapping = new Dictionary<TCGTransType, Lazy<string>>
            {
                {
                    TCGTransType.Debit, new Lazy<string>(() =>
                        @$"SELECT * FROM TSDB.BettransLottery_TCG WHERE ref_no='{inputs["DebitID"]}'")
                },
                {
                    TCGTransType.Credit, new Lazy<string>(() =>
                        @$"SELECT * FROM TSDB.BettransLottery_TCG WHERE ref_no='{inputs["CreditID"]}'")
                },
                {
                    TCGTransType.Reserve, new Lazy<string>(() =>
                        @$"SELECT * FROM TSDB.BettransLottery_TCG WHERE ref_no='{inputs["ReserveID"]}'")
                },
            };
        }

        public TCGBLLSandbox GetBalance()
        {
            var sParam = new
            {
                method = "sgb",
                username = _tpUserName,
                product_type = "384",//420,
                request_time = DateTime.Now.ToFileTime(),
            };

            var (merchantCode, @params, sign) = GetRequest(sParam, _merchantCode);
            Result = _target.SeamlessWallet(merchantCode, @params, sign)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            Balance = (TCGBalanceObj)Result;
            return this;
        }

        public TCGBLLSandbox Debit(Dictionary<string, string> inputs, decimal amount)
        {
            SeamlessWallet(TCGApiMethod.Debit, inputs, amount, inputs["DebitID"]);
            return this;
        }


        public TCGBLLSandbox Credit(Dictionary<string, string> inputs, decimal amount)
        {
            SeamlessWallet(TCGApiMethod.Credit, inputs, amount, inputs["CreditID"]);
            return this;
        }

        public TCGBLLSandbox Reserve(Dictionary<string, string> inputs, decimal amount)
        {
            SeamlessWallet(TCGApiMethod.Reserve, inputs, amount, inputs["ReserveID"]);
            return this;
        }

        public void SeamlessWallet(string method, Dictionary<string, string> inputs, decimal amount, string ref_no)
        {
            var sParam = new
            {
                method = method,
                product_type = 384,
                transactions = new[]
                   {
                    new
                    {
                        ref_no = ref_no,
                        username = inputs["TpUserName"],
                        amount = amount,
                        tx_type = "3250",
                        remarks = "test",
                        additionalInformation= new
                        {
                            order_no = inputs["OrderNo"],
                        }
                    }
                },
                request_time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            };

            var (merchantCode, @params, sign) = GetRequest(sParam, _merchantCode);
            Result = _target.SeamlessWallet(merchantCode, @params, sign)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public TCGBLLSandbox CancelReserve(Dictionary<string, string> inputs, decimal amount)
        {
            var sParam = new
            {
                method = TCGApiMethod.CancelReserve,
                product_type = 384,
                reservation_details = new[]
                {
                    new
                    {
                        ref_no = inputs["ReserveID"],
                        username= _tpUserName,
                        amount,
                        additionalInformation= new
                        {
                            order_no = inputs["OrderNo"],
                        }
                    }
                },
                request_time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            };

            var (merchantCode, @params, sign) = GetRequest(sParam, _merchantCode);
            Result = _target.SeamlessWallet(merchantCode, @params, sign)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return this;
        }

        public TCGBLLSandbox ConfirmReserve(Dictionary<string, string> inputs, decimal amount)
        {
            var sParam = new
            {
                method = TCGApiMethod.ConfirmReserve,
                product_type = 384,
                reservation_details = new[]
                {
                    new
                    {
                        ref_no = inputs["ReserveID"],
                        username= _tpUserName,
                        amount,
                        additionalInformation= new
                        {
                            order_no = inputs["OrderNo"],
                        }
                    }
                },
                request_time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            };

            var (merchantCode, @params, sign) = GetRequest(sParam, _merchantCode);
            Result = _target.SeamlessWallet(merchantCode, @params, sign)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return this;
        }

        public TCGBLLSandbox CheckTransactionStatus(string _transId, decimal amount)
        {
            var sParam = new
            {
                method = TCGApiMethod.CheckTransactionStatus,
                product_type = 384,
                transaction_details = new[]
                {
                    new
                    {
                        ref_no = _transId,
                        username= _tpUserName,
                        amount,
                    }
                },
                request_time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            };

            var (merchantCode, @params, sign) = GetRequest(sParam, _merchantCode);
            Result = _target.SeamlessWallet(merchantCode, @params, sign)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            Check = (TCGCheckObj)Result;
            return this;
        }

        public TCGBLLSandbox ResultShouldBe(int expected)
        {
            Result.status.Should().Be(expected.ToString());
            return this;
        }

        public TCGBLLSandbox ShouldBeHaveRecord(TCGTransType transType, Dictionary<string, string> inputs, decimal amount)
        {
            var expected = CreateTCGRecord(transType, inputs, amount);
            var sqlScript = _exceptSqlMapping[transType].Value;
            var actual = _mySqlClient.Query<TCGRecord>(sqlScript, CommandType.Text).First();
            actual.Should().BeEquivalentTo(expected);
            return this;
        }

        public TCGBLLSandbox BalanceShouldBe(decimal expected)
        {
            Balance.status.Should().Be(TCGReturnCode.Success.ToString());
            Balance.balance.Should().Be(expected.ToString());
            return this;
        }

        public TCGBLLSandbox StatusShouldBe(string expected)
        {
            Check.status.Should().Be(TCGReturnCode.Success.ToString());
            Check.query_result.First().trans_status.Should().Be(expected.ToString());
            return this;
        }

        private TCGRecord CreateTCGRecord(TCGTransType transType, Dictionary<string, string> inputs, decimal amount)
        {
            var ref_no = string.Empty;
            switch (transType)
            {
                case TCGTransType.Debit:
                    ref_no = inputs["DebitID"];
                    break;
                case TCGTransType.Credit:
                    ref_no = inputs["CreditID"];
                    break;
                case TCGTransType.Reserve:
                    ref_no = inputs["ReserveID"];
                    break;
                default:
                    break;
            }
            return new TCGRecord
            {
                TransType = transType.ToString(),
                username = inputs["TpUserName"],
                ref_no = ref_no,
                amount = amount,
            };
        }

        private string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        private (string, string, string) GetRequest(object sParam, string merchantCode)
        {
            var tcgConfig = new TCGConfig(_target.thirdPartySetting.TpSetting, merchantCode);
            var hashString = ToJson(sParam);

            //Act
            var desEncrypt = _target.DesEncrypt(hashString, tcgConfig.DesKey);
            var sign = _target.SHA256($"{desEncrypt}{tcgConfig.SignKey}");
            return (merchantCode, desEncrypt, sign);
        }
    }
}
