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
		private readonly Queue<TrackLodestoneRequest> _idRequests = new Queue<TrackLodestoneRequest>();
		private readonly Queue<TrackLodestoneResponse> _idResponses = new Queue<TrackLodestoneResponse>();
		private readonly Timer _onRequestTimer;
		private readonly IPlayerTrackPlugin _playerTrackPlugin;
		private readonly Queue<TrackLodestoneRequest> _updateRequests = new Queue<TrackLodestoneRequest>();
		private readonly Queue<TrackLodestoneResponse> _updateResponses = new Queue<TrackLodestoneResponse>();
		private bool _isProcessing;
		private DateTime _lodestoneCooldown = DateTime.UtcNow;

		public LodestoneService(IPlayerTrackPlugin playerTrackPlugin)
		{
			var httpClientHandler = new HttpClientHandler();
			_playerTrackPlugin = playerTrackPlugin;
			_httpClient = new HttpClient(httpClientHandler, true)
			{
				Timeout = TimeSpan.FromMilliseconds(_playerTrackPlugin.Configuration.LodestoneTimeout)
			};
			_onRequestTimer = new Timer
				{Interval = 15000, Enabled = true};
			_onRequestTimer.Elapsed += ProcessRequest;
		}

		public List<TrackLodestoneResponse> GetVerificationResponses()
		{
			var responses = new List<TrackLodestoneResponse>();
			while (_idResponses.Count > 0) responses.Add(_idResponses.Dequeue());

			return responses;
		}

		public List<TrackLodestoneResponse> GetUpdateResponses()
		{
			var responses = new List<TrackLodestoneResponse>();
			while (_updateResponses.Count > 0) responses.Add(_updateResponses.Dequeue());

			return responses;
		}

		public void AddIdRequest(TrackLodestoneRequest request)
		{
			try
			{
				if (_idRequests.Any(existingRequest => existingRequest.PlayerKey == request.PlayerKey)) return;
				_idRequests.Enqueue(request);
			}
			catch
			{
				// ignored
			}
		}

		public void AddUpdateRequest(TrackLodestoneRequest request)
		{
			try
			{
				if (_updateRequests.Any(existingRequest => existingRequest.PlayerKey == request.PlayerKey)) return;
				_updateRequests.Enqueue(request);
			}
			catch
			{
				// ignored
			}
		}

		public void Dispose()
		{
			_onRequestTimer.Elapsed -= ProcessRequest;
			_onRequestTimer.Stop();
			_httpClient.Dispose();
		}

		private void ProcessIdRequest()
		{
			var request = _idRequests.Peek();
			var requestCount = 0;
			while (requestCount < _playerTrackPlugin.Configuration.LodestoneMaxRetry)
			{
				var response = GetCharacterId(request);
				if (response.Status == TrackLodestoneStatus.Verified)
				{
					_idRequests.Dequeue();
					_idResponses.Enqueue(response);
					return;
				}

				Thread.Sleep(_playerTrackPlugin.Configuration.LodestoneRequestDelay);
				requestCount++;
			}

			if (requestCount >= _playerTrackPlugin.Configuration.LodestoneMaxRetry)
			{
				if (IsLodestoneAvailable())
				{
					var response = new TrackLodestoneResponse
					{
						PlayerKey = request.PlayerKey,
						Status = TrackLodestoneStatus.Failed
					};
					_idRequests.Dequeue();
					_idResponses.Enqueue(response);
				}
				else
				{
					_lodestoneCooldown =
						DateTime.UtcNow.AddMilliseconds(_playerTrackPlugin.Configuration.LodestoneCooldownDuration);
				}
			}
		}

		private void ProcessUpdateRequest()
		{
			var request = _updateRequests.Peek();
			var requestCount = 0;
			while (requestCount < _playerTrackPlugin.Configuration.LodestoneMaxRetry)
			{
				var response = GetCharacterProfile(request);
				if (response.Status == TrackLodestoneStatus.Updated)
				{
					response.Status = TrackLodestoneStatus.Verified;
					_updateRequests.Dequeue();
					_updateResponses.Enqueue(response);
					return;
				}

				Thread.Sleep(_playerTrackPlugin.Configuration.LodestoneRequestDelay);
				requestCount++;
			}

			if (requestCount >= _playerTrackPlugin.Configuration.LodestoneMaxRetry)
			{
				if (IsLodestoneAvailable())
				{
					var response = new TrackLodestoneResponse
					{
						PlayerKey = request.PlayerKey,
						Status = TrackLodestoneStatus.Failed
					};
					_playerTrackPlugin.LogInfo("Setting " + request.PlayerKey + " to failed.");
					_updateRequests.Dequeue();
					_idResponses.Enqueue(response);
				}
				else
				{
					_lodestoneCooldown =
						DateTime.UtcNow.AddMilliseconds(_playerTrackPlugin.Configuration.LodestoneCooldownDuration);
				}
			}
		}

		private void ProcessRequest(object source, ElapsedEventArgs e)
		{
			if (_isProcessing) return;
			_isProcessing = true;
			while (_idRequests.Count > 0 && DateTime.UtcNow > _lodestoneCooldown)
			{
				ProcessIdRequest();
				Thread.Sleep(_playerTrackPlugin.Configuration.LodestoneRequestDelay);
			}

			while (_updateRequests.Count > 0 && DateTime.UtcNow > _lodestoneCooldown)
			{
				ProcessUpdateRequest();
				Thread.Sleep(_playerTrackPlugin.Configuration.LodestoneRequestDelay);
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
				var worldId = _playerTrackPlugin.GetWorldId(json.Character.Server.ToString());
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
			          "&server=" + worldName;
			return await _httpClient.GetAsync(new Uri(url));
		}

		private async Task<HttpResponseMessage> GetCharacterProfileAsync(uint lodestoneId)
		{
			var url = "https://xivapi.com/character/" + lodestoneId;
			return await _httpClient.GetAsync(new Uri(url));
		}
	}
}