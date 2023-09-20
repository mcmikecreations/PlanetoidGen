using Microsoft.AspNetCore.Mvc;
using PlanetoidGen.Contracts.Repositories.Messaging;

namespace PlanetoidGen.API.Controllers.System
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagingController : ControllerBase
    {
        private readonly IGenerationJobMessageAdminRepository _adminRepository;
        private readonly ILogger<MessagingController> _logger;

        public MessagingController(
            IGenerationJobMessageAdminRepository kafkaAdminRepository,
            ILogger<MessagingController> logger)
        {
            _adminRepository = kafkaAdminRepository ?? throw new ArgumentNullException(nameof(kafkaAdminRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("topics/agents/reset")]
        public async Task<IActionResult> ResetAgentTopics()
        {
            var getResult = await _adminRepository.GetAllTopics();

            if (!getResult.Success)
            {
                _logger.LogError("GetTopics error: {error}", getResult.ErrorMessage!.ToString());
                return Problem(detail: getResult.ErrorMessage!.ToString(), statusCode: StatusCodes.Status500InternalServerError);
            }

            var topicsToDelete = getResult.Data
                .Where(x => x != null && x.StartsWith(_adminRepository.AgentTopicNamePrefix))
                .ToList();

            var deleteResult = await _adminRepository.DeleteTopics(topicsToDelete);

            if (!deleteResult.Success)
            {
                _logger.LogError("GetTopics error: {error}", deleteResult.ErrorMessage!.ToString());
                return Problem(detail: deleteResult.ErrorMessage!.ToString(), statusCode: StatusCodes.Status500InternalServerError);
            }

            var createResult = await _adminRepository.CreateTopics(_adminRepository.GetAgentTopics(1));

            if (!createResult.Success)
            {
                _logger.LogError("GetTopics error: {error}", createResult.ErrorMessage!.ToString());
                return Problem(detail: createResult.ErrorMessage!.ToString(), statusCode: StatusCodes.Status500InternalServerError);
            }

            return Ok(createResult.Data);
        }

        [HttpGet("topics")]
        public async Task<IActionResult> GetAllTopics()
        {
            var getResult = await _adminRepository.GetAllTopics();

            if (!getResult.Success)
            {
                _logger.LogError("GetTopics error: {error}", getResult.ErrorMessage!.ToString());
                return Problem(detail: getResult.ErrorMessage!.ToString(), statusCode: StatusCodes.Status500InternalServerError);
            }

            return Ok(getResult.Data);
        }

        [HttpDelete("topics")]
        public async Task<IActionResult> DeleteAllTopics()
        {
            var deleteResult = await _adminRepository.DeleteAllTopics();

            if (!deleteResult.Success)
            {
                _logger.LogError("GetTopics error: {error}", deleteResult.ErrorMessage!.ToString());
                return Problem(detail: deleteResult.ErrorMessage!.ToString(), statusCode: StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }
    }
}
