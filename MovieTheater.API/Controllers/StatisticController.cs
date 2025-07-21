using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.StatisticDTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/statistic")]
    [ApiController]
    public class StatisticController : ControllerBase
    {
        private readonly IStatisticService _statisticService;

        public StatisticController(IStatisticService statisticService)
        {
            _statisticService = statisticService;
        }

        [HttpGet("monthly-register")]
        [SwaggerOperation(
            Summary = "Get monthly user registration statistics",
            Description = "Retrieves the number of user registrations per month."
        )]
        [ProducesResponseType(typeof(ApiResult<List<MonthlyRegisterDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetRegisterPerMonthAsync()
        {
            try
            {
                var result = await _statisticService.GetRegisterPerMonthAsync();
                return Ok(ApiResult<List<MonthlyRegisterDto>>.Success(result, "200", "Monthly registration statistics retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        // Add more endpoints here following the MovieController pattern as needed.
    }
}
