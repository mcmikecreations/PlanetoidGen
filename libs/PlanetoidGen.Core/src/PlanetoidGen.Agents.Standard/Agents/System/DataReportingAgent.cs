using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanetoidGen.BusinessLogic.Agents.Models.Agents;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Standard.Agents.System
{
    public class DataReportingAgent : ITypedAgent<AgentEmptySettings>
    {
        private string? _mainApiBaseUrl;
        private ILogger? _logger;

        public string Title => $"{nameof(PlanetoidGen)}.{nameof(DataReportingAgent)}";

        public string Description => string.Empty;

        public bool IsVisibleToClient => false;

        public async ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token)
        {
            try
            {
                _logger!.LogDebug("DataReportingAgent sends request: {payload}", JsonSerializer.Serialize(job));

                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                    {
                        return true;
                    }
                };

                using (var client = new HttpClient(httpClientHandler))
                {
                    client.BaseAddress = new Uri(_mainApiBaseUrl);
                    var content = JsonSerializer.Serialize(job);
                    var byteContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "api/data/report")
                    {
                        Version = HttpVersion.Version20,
                        Content = byteContent
                    });

                    _logger!.LogDebug("DataReportingAgent received response with status code: {code}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError("DataReportingAgent error: {error}", ex.ToString());
                return Result.CreateFailure("DataReportingAgent error: {error}", ex.ToString());
            }

            return Result.CreateSuccess();
        }

        public AgentEmptySettings GetTypedDefaultSettings()
        {
            return AgentEmptySettings.Default;
        }

        public ValueTask<string> GetDefaultSettings()
        {
            return GetTypedDefaultSettings().Serialize();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies(int z)
        {
            return GetDependencies();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies()
        {
            return new ValueTask<IEnumerable<AgentDependencyModel>>(Array.Empty<AgentDependencyModel>());
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z)
        {
            return GetOutputs();
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs()
        {
            return new ValueTask<IEnumerable<DataTypeInfoModel>>(Array.Empty<DataTypeInfoModel>());
        }

        public ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider)
        {
            try
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();

                _mainApiBaseUrl = config["MainApiBaseUrl"] ?? throw new ArgumentException("'MainApiBaseUrl' is not specified in appsettings.json");
                _logger = serviceProvider.GetService<ILogger<DataReportingAgent>>();
            }
            catch (Exception ex)
            {
                return new ValueTask<Result>(Result.CreateFailure(ex));
            }

            return new ValueTask<Result>(Result.CreateSuccess());
        }
    }
}
