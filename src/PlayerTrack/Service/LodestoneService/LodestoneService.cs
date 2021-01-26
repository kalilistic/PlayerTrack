// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InvertIf

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace PlayerTrack
{
	public class LodestoneService : ILodestoneService
	{
		private readonly HttpClient _httpClient;
		private readonly Timer _onRequestTimer;
		private readonly IPlayerTrackPlugin _plugin;
		private readonly Queue<TrackLodestoneRequest> _requests = new Queue<TrackLodestoneRequest>();
		private readonly Queue<TrackLodestoneResponse> _responses = new Queue<TrackLodestoneResponse>();
		private bool _isProcessing;
		private DateTime _lodestoneCooldown = DateTime.UtcNow;

		public LodestoneService(IPlayerTrackPlugin plugin)
		{
			var httpClientHandler = new HttpClientHandler();
			_plugin = plugin;
			_httpClient = new HttpClient(httpClientHandler, true)
			{
				Timeout = TimeSpan.FromMilliseconds(_plugin.Configuration.LodestoneTimeout)
			};
			_onRequestTimer = new Timer
				{Interval = 15000, Enabled = true};
			_onRequestTimer.Elapsed += ProcessRequests;
		}

		public List<TrackLodestoneResponse> GetResponses()
		{
			var responses = new List<TrackLodestoneResponse>();
			while (_responses.Count > 0) responses.Add(_responses.Dequeue());
			return responses;
		}

		public void AddRequest(TrackLodestoneRequest request)
		{
			try
			{
				if (_requests.Any(existingRequest => existingRequest.PlayerKey == request.PlayerKey)) return;
				_requests.Enqueue(request);
			}
			catch
			{
				// ignored
			}
		}

		public void Dispose()
		{
			_onRequestTimer.Elapsed -= ProcessRequests;
			_onRequestTimer.Stop();
			_httpClient.Dispose();
		}

		private void ProcessRequest()
		{
			var request = _requests.Peek();
			var requestCount = 0;
			while (requestCount < _plugin.Configuration.LodestoneMaxRetry)
			{
				var response = GetCharacterId(request);
				if (response.Status == TrackLodestoneStatus.Verified)
				{
					_requests.Dequeue();
					_responses.Enqueue(response);
					return;
				}

				Thread.Sleep(_plugin.Configuration.LodestoneRequestDelay);
				requestCount++;
			}

			if (requestCount >= _plugin.Configuration.LodestoneMaxRetry)
			{
				if (IsLodestoneAvailable())
				{
					var response = new TrackLodestoneResponse
					{
						PlayerKey = request.PlayerKey,
						Status = TrackLodestoneStatus.Failed
					};
					_requests.Dequeue();
					_responses.Enqueue(response);
				}
				else
				{
					_lodestoneCooldown =
						DateTime.UtcNow.AddMilliseconds(_plugin.Configuration.LodestoneCooldownDuration);
				}
			}
		}

		private void ProcessRequests(object source, ElapsedEventArgs e)
		{
			if (_isProcessing) return;
			_isProcessing = true;
			while (_requests.Count > 0 && DateTime.UtcNow > _lodestoneCooldown)
			{
				ProcessRequest();
				Thread.Sleep(_plugin.Configuration.LodestoneRequestDelay);
			}

			_isProcessing = false;
		}

		private bool IsLodestoneAvailable()
		{
			var request = new TrackLodestoneRequest
			{
				PlayerKey = "HEALTH_CHECK",
				LodestoneId = 1
			};
			var result = GetCharacterProfile(request);
			return !string.IsNullOrEmpty(result?.PlayerName);
		}

		private TrackLodestoneResponse GetCharacterId(TrackLodestoneRequest request)
		{
			var response = new TrackLodestoneResponse();
			try
			{
				var result = GetCharacterIdAsync(request.PlayerName, request.WorldName).Result;
				if (result.StatusCode != HttpStatusCode.OK) return response;
				var json = JsonConvert.DeserializeObject<dynamic>(result.Content.ReadAsStringAsync().Result);
				if (json == null) return response;
				if (json.Results == null) return response;
				if (json.Results[0] == null) return response;
				response.Status = TrackLodestoneStatus.Verified;
				response.LodestoneId = (uint) json.Results[0].ID;
				response.PlayerKey = request.PlayerKey;
				return response;
			}
			catch
			{
				return response;
			}
		}

		private TrackLodestoneResponse GetCharacterProfile(TrackLodestoneRequest request)
		{
			var response = new TrackLodestoneResponse();
			try
			{
				var result = GetCharacterProfileAsync(request.LodestoneId).Result;
				if (result.StatusCode != HttpStatusCode.OK) return response;
				var json = JsonConvert.DeserializeObject<dynamic>(result.Content.ReadAsStringAsync().Result);
				if (json == null) return response;
				if (json.Character == null) return response;
				if (json.Character?.Name == null) return response;
				if (json.Character?.Server == null) return response;
				var worldId = _plugin.GetWorldId(json.Character.Server.ToString());
				if (worldId == null) return response;
				var homeWorld = new TrackWorld {Id = worldId, Name = json.Character.Server};
				response.Status = TrackLodestoneStatus.Updated;
				response.PlayerName = json.Character.Name;
				response.HomeWorld = homeWorld;
				response.PlayerKey = request.PlayerKey;
				return response;
			}
			catch
			{
				return response;
			}
		}

		private async Task<HttpResponseMessage> GetCharacterIdAsync(string characterName, string worldName)
		{
			var url = "https://xivapi.com/character/search?name=" + characterName +
			          "&server=" + worldName + "&columns=ID";
			return await _httpClient.GetAsync(new Uri(url));
		}

		private async Task<HttpResponseMessage> GetCharacterProfileAsync(uint lodestoneId)
		{
			var url = "https://xivapi.com/character/" + lodestoneId + "?columns=Character.Name,Character.Server";
			return await _httpClient.GetAsync(new Uri(url));
		}
	}
}