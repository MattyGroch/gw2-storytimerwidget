using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Blish_HUD;
using GW2StoryTimes.Models;
using Newtonsoft.Json;

namespace GW2StoryTimes.Services
{
    public class StoryTimesApiClient : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<StoryTimesApiClient>();

        private const string BaseUrl = "https://api.gw2storytimes.com/v1";

        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<int, CachedItem<Mission>> _missionCache = new ConcurrentDictionary<int, CachedItem<Mission>>();
        private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(15);

        private List<Season> _seasons;

        public StoryTimesApiClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GW2StoryTimes-BlishHUD/0.1.0");
        }

        public async Task PreloadSeasonsAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("/v1/seasons");
                _seasons = JsonConvert.DeserializeObject<List<Season>>(response);
                Logger.Info($"Loaded {_seasons?.Count ?? 0} seasons from Story Times API.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to preload seasons: {ex.Message}");
                _seasons = new List<Season>();
            }
        }

        public List<Season> GetCachedSeasons() => _seasons ?? new List<Season>();

        public async Task<Season> GetSeasonAsync(string seasonId)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"/v1/seasons/{seasonId}");
                return JsonConvert.DeserializeObject<Season>(response);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to get season {seasonId}: {ex.Message}");
                return null;
            }
        }

        public async Task<Mission> GetMissionAsync(int missionId)
        {
            if (_missionCache.TryGetValue(missionId, out var cached) && !cached.IsExpired(_cacheTtl))
            {
                return cached.Value;
            }

            try
            {
                var response = await _httpClient.GetStringAsync($"/v1/missions/{missionId}");
                var mission = JsonConvert.DeserializeObject<Mission>(response);
                _missionCache[missionId] = new CachedItem<Mission>(mission);
                return mission;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to get mission {missionId}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SubmitTimeAsync(int missionId, string category, double durationMins)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(new
                {
                    category,
                    duration_mins = Math.Round(durationMins, 2),
                    source = "blishhud"
                });

                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"/v1/missions/{missionId}/submit", content);

                if (response.IsSuccessStatusCode)
                {
                    Logger.Info($"Submitted time for mission {missionId}: {durationMins:F1} min ({category})");
                    _missionCache.TryRemove(missionId, out _);
                    return true;
                }

                Logger.Warn($"Submission failed for mission {missionId}: HTTP {(int)response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Submission error for mission {missionId}: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        private class CachedItem<T>
        {
            public T Value { get; }
            public DateTime CachedAt { get; }

            public CachedItem(T value)
            {
                Value = value;
                CachedAt = DateTime.UtcNow;
            }

            public bool IsExpired(TimeSpan ttl) => DateTime.UtcNow - CachedAt > ttl;
        }
    }
}
