using Microsoft.AspNetCore.Mvc;
using PlanetoidGen.API.Helpers.Abstractions;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using System.Collections.Concurrent;

namespace PlanetoidGen.API.Controllers.System
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly IStreamContext<GenerationJobMessage> _streamContext;
        private readonly ILogger<DataController> _logger;

        public DataController(
            IStreamContext<GenerationJobMessage> streamContext,
            ILogger<DataController> logger)
        {
            _streamContext = streamContext;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("report")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Report(GenerationJobMessage job)
        {
            try
            {
                _logger.LogInformation($"Received message: {job.Id} for connection {job.ConnectionId}");

                var messages = _streamContext.StreamMessages!.GetOrAdd(job.ConnectionId!, new ConcurrentBag<GenerationJobMessage>());
                messages.Add(job);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex);
            }
        }
    }
}
