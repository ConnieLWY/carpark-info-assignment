using Microsoft.AspNetCore.Mvc;
using HandshakesByDC_BEAssignment.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HandshakesByDC_BEAssignment.Controllers.v1
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarparkImportController : ControllerBase
    {
        private readonly SampleData _sampleData;
        private readonly ILogger<CarparkImportController> _logger;

        public CarparkImportController(SampleData sampleData, ILogger<CarparkImportController> logger)
        {
            _sampleData = sampleData;
            _logger = logger;
        }

        [HttpGet("messages")]
        public ActionResult<IEnumerable<string>> GetMessages()
        {
            _logger.LogInformation("Retrieving messages from sample data");

            var sortedLogs = _sampleData.Logs
                .Select(log =>
                {
                    // Extract timestamp from the start of the message
                    var timestampStr = log.Substring(0, 23); // "MM/dd/yyyy HH:mm:ss.fff" is 23 characters

                    return new
                    {
                        Message = log,
                        Timestamp = DateTime.TryParseExact(
                            timestampStr,
                            "MM/dd/yyyy HH:mm:ss.fff",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var dt) ? dt : DateTime.MinValue,
                        IsError = log.Contains("Error"),
                        IsReading = log.Contains("Reading"),
                        IsProcessed = log.Contains("processed")
                    };
                })
                .OrderByDescending(x => x.Timestamp)
                .ThenBy(x => x.IsError)  // Show errors last for same timestamp
                .ThenBy(x => x.IsProcessed)  // Show processed messages next
                .ThenBy(x => x.IsReading)    // Show reading messages first
                .Select(x => x.Message)
                .ToList();

            return Ok(sortedLogs);
        }
    }
}