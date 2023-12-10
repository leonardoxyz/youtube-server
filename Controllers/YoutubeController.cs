using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using youtubeAPI.Models;
using System;
using System.Linq;

namespace youtubeAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeController : Controller
    {
        private readonly ILogger<YoutubeController> _logger;

        public YoutubeController(ILogger<YoutubeController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetChannelVideos")]
        public async Task<IActionResult> GetChannelVideos([FromQuery] string channelUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channelUrl))
                {
                    return BadRequest("A URL do canal não pode ser nula ou vazia.");
                }

                var channelId = ExtractChannelId(channelUrl);

                if (string.IsNullOrWhiteSpace(channelId))
                {
                    return BadRequest("A URL do canal é inválida.");
                }

                var youtubeService = new YouTubeService(new BaseClientService.Initializer
                {
                    ApiKey = "Your API key here!",
                    ApplicationName = "MyYoutubeApp",
                });

                var searchRequest = youtubeService.Search.List("snippet");
                searchRequest.ChannelId = channelId;
                searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchRequest.MaxResults = 10;

                var searchResponse = await searchRequest.ExecuteAsync();

                var videoList = searchResponse.Items.Select(item => new VideoDetails
                {
                    Title = item.Snippet.Title,
                    Link = $"https://www.youtube.com/watch?v={item.Id.VideoId}",
                    Thumbnail = item.Snippet.Thumbnails.Medium.Url,
                    PublishedAt = item.Snippet.PublishedAtDateTimeOffset,
                    Duration = GetVideoDuration(item.Id.VideoId, youtubeService)
                })
                .OrderByDescending(video => video.PublishedAt).ToList();

                return Ok(videoList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter vídeos");
                return StatusCode(500, "Internal Server Error. Consulte os logs para mais detalhes.");
            }
        }

        private string GetVideoDuration(string videoId, YouTubeService youtubeService)
        {
            try
            {
                var videoRequest = youtubeService.Videos.List("contentDetails");
                videoRequest.Id = videoId;

                var videoResponse = videoRequest.Execute();

                if (videoResponse.Items.Count > 0)
                {
                    var duration = videoResponse.Items[0].ContentDetails.Duration;
                    return ParseYouTubeDuration(duration);
                }

                return "N/A";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a duração do vídeo");
                return "N/A";
            }
        }

        private string ParseYouTubeDuration(string duration)
        {
            try
            {
                var timeSpan = System.Xml.XmlConvert.ToTimeSpan(duration);
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao analisar a duração do vídeo");
                return "N/A";
            }
        }

        private string ExtractChannelId(string channelUrl)
        {
            if (channelUrl.Contains("/c/"))
            {
                var segments = channelUrl.Split('/');
                var index = Array.IndexOf(segments, "c");

                if (index != -1 && index + 1 < segments.Length)
                {
                    return segments[index + 1];
                }
            }

            if (channelUrl.Contains("/channel/"))
            {
                var segments = channelUrl.Split('/');
                var index = Array.IndexOf(segments, "channel");

                if (index != -1 && index + 1 < segments.Length)
                {
                    return segments[index + 1];
                }
            }

            var uri = new Uri(channelUrl);
            var channelId = uri.Query
                .Split('&')
                .Select(part => part.Split('='))
                .FirstOrDefault(pair => pair.Length == 2 && pair[0].Equals("channel_id", StringComparison.OrdinalIgnoreCase));

            return channelId?[1];
        }
    }
}
