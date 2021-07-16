using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.Stubs;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;

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

        private readonly string _transId;

        private Dictionary<Type, Lazy<string>> _exceptSqlMapping;

        private readonly TCGBLL _target;

        public TCGReturnObj Result;

        public TCGBalanceObj Balance;

        public TCGCheckObj Check;

        public TCGBLLSandbox(string merchantCode, string tpUserName, string transId)
        {
            _merchantCode = merchantCode;
            _tpUserName = tpUserName;
            _transId = transId;
            _serviceCollection = new ServiceCollection();
            _serviceCollection = SetEnvironment(_serviceCollection);
            _serviceCollection = LoadConfig(_serviceCollection);

            _serviceCollection.AddTransient(p => Substitute.For<ILogger<SettledService>>());
            _serviceCollection.AddTransient(p => Substitute.For<ILogger<BettingService>>());
            var mySqlLog = Substitute.For<ILogger<MySqlClient>>();
            _serviceCollection.AddTransient(p => mySqlLog);
            var conn = _configuration.GetConnectionString("ConnectionString");
            _serviceCollection.AddSingleton(new MySqlClient(conn, mySqlLog));

            var startUp = new StubStartUp(_configuration);
            startUp.ConfigureServices(_serviceCollection);
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _mySqlClient = _serviceProvider.GetRequiredService<MySqlClient>();
            _target = new TCGBLL(_serviceProvider);

            _exceptSqlMapping = new Dictionary<Type, Lazy<string>>
            {
                {
                    typeof(TCGRecord), new Lazy<string>(() =>
                        @$"SELECT * FROM BettransLottery_TCG WHERE ref_no=""{_transId}""")
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

        public TCGBLLSandbox Debit(string _transId, decimal amount)
        {
            var sParam = new
            {
                method = "db",
                product_type = 384,
                transactions = new[]
                {
                    new
                    {
                        ref_no = _transId,
                        username = _tpUserName,
                        amount = amount,
                        tx_type = "3250",
                        remarks = "test",
                        additionalInformation= new
                        {
                            order_no = "HV102753908ZIP6GA",
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

        public TCGBLLSandbox Credit(string _transId, decimal amount)
        {
            var sParam = new
            {
                method = "cr",
                product_type = 384,
                transactions = new[]
                {
                    new
                    {
                        ref_no = _transId,
                        username= _tpUserName,
                        amount,
                        tx_type= "3250",
                        remarks= "test",
                        additionalInformation= new
                        {
                           order_no = "HV102753908ZIP6GA",
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

        public TCGBLLSandbox Reserve(string _transId, decimal amount)
        {
            var sParam = new
            {
                method = "rsv",
                product_type = 384,
                transactions = new[]
                {
                    new
                    {
                        ref_no = _transId,
                        username= _tpUserName,
                        amount,
                        tx_type= "3250",
                        remarks= "test",
                        additionalInformation= new
                        {
                            order_no = "HV102753908ZIP6GA",
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

        public TCGBLLSandbox CancelReserve(string _transId, decimal amount)
        {
            var sParam = new
            {
                method = "cnrsv",
                product_type = 384,
                reservation_details = new[]
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
            return this;
        }

        public TCGBLLSandbox ConfirmReserve(string _transId, decimal amount)
        {
            var sParam = new
            {
                method = "crsv",
                product_type = 384,
                reservation_details = new[]
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
            return this;
        }

        public TCGBLLSandbox CheckTransactionStatus(string _transId, decimal amount)
        {
            var sParam = new
            {
                method = "scs",
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

        public TCGBLLSandbox ShouldBeHaveRecord<TRecord>(Lazy<TRecord> data)
            where TRecord : class
        {
            var sqlScript = _exceptSqlMapping[typeof(TRecord)].Value;
            var actual = _mySqlClient.Query<TRecord>(sqlScript, CommandType.Text).First();
            actual.Should().BeEquivalentTo(data.Value);
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

        public static Lazy<TCGRecord> TCGRecord(TCGTransType tCGTransType, string tpUserName, string ref_no, decimal amount) =>
        new Lazy<TCGRecord>(() => new TCGRecord
        {
            TransType = tCGTransType.ToString(),
            username = tpUserName,
            ref_no = ref_no,
            amount = amount,          
            //TicketType = 0,
            //TicketStatus = 0,
            //SettleFlag = 0,
        });

        private ServiceCollection SetEnvironment(ServiceCollection services)
        {
            var env = new HostingEnvironment()
            {
                EnvironmentName = Environments.Development,
            };
            services.AddSingleton<IHostEnvironment>(env);
            return services;
        }

        private ServiceCollection LoadConfig(ServiceCollection services)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .AddJsonFile("appsettings.Local.json", false, false);
            _configuration = configBuilder.Build();
            services.AddSingleton(_configuration);
            return services;
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
