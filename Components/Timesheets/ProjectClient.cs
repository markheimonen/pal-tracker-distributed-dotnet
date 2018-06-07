using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System;

namespace Timesheets
{
    public class ProjectClient : IProjectClient
    {
        private readonly HttpClient _client;
        private Dictionary<long, ProjectInfo> _cache;
        private readonly ILogger<ProjectClient> _logger;
        private readonly Func<Task<string>> _accessTokenFn;

        public ProjectClient(HttpClient client, ILogger<ProjectClient> logger, Func<Task<string>> accessTokenFn)
        {
            _client = client;
            _cache = new Dictionary<long, ProjectInfo>();
            _logger = logger;
            _accessTokenFn = accessTokenFn;
        }

        public async Task<ProjectInfo> Get(long projectId) =>
         await new GetProjectCommand(DoGet, DoGetFromCache, projectId).ExecuteAsync();

        public async Task<ProjectInfo> DoGet(long projectId)
        {
               var token = await _accessTokenFn();

            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
            var streamTask = _client.GetStreamAsync($"project?projectId={projectId}");
            _logger.LogInformation($"Attempting to fetch projectId: {projectId}");

            var serializer = new DataContractJsonSerializer(typeof(ProjectInfo));
            var data = serializer.ReadObject(await streamTask) as ProjectInfo;
            if (_cache.ContainsKey(projectId))
            {
                _cache[projectId] = data;
            }
            else 
            {
                _cache.Add(projectId, data);
            }
            return data;
        }

        public Task<ProjectInfo> DoGetFromCache(long projectId)
        {          
           return Task.FromResult(_cache[projectId]);                 
        }
    }
}