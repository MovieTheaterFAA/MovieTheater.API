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

        [HttpGet("monthly-revenue")]
        [SwaggerOperation(
        Summary = "Get monthly ticket revenue statistics",
        Description = "Retrieves the total ticket revenue per month."
        )]
        [ProducesResponseType(typeof(ApiResult<List<MonthlyRevenueDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetMonthlyRevenueAsync()
        {
            try
            {
                var result = await _statisticService.GetMonthlyRevenueAsync();
                return Ok(ApiResult<List<MonthlyRevenueDto>>.Success(result, "200", "Monthly revenue statistics retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("monthly-movie-revenue")]
        [SwaggerOperation(
            Summary = "Get monthly revenue for each movie",
            Description = "Retrieves the total ticket revenue per movie for a specified month and year."
        )]
        [ProducesResponseType(typeof(ApiResult<List<MonthlyMovieRevenueDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetMonthlyRevenueMovieAsync([FromQuery] MonthYearDto monthYear)
        {
            try
            {
                if (monthYear.Month < 1 || monthYear.Month > 12)
                    return BadRequest(ApiResult<object>.Failure("400", "Month must be between 1 and 12."));
                if (monthYear.Year < 2000 || monthYear.Year > DateTime.UtcNow.Year)
                    return BadRequest(ApiResult<object>.Failure("400", "Year is out of valid range."));

                var result = await _statisticService.GetMonthlyRevenueMovieAsync(monthYear);
                return Ok(ApiResult<List<MonthlyMovieRevenueDto>>.Success(result, "200", "Monthly movie revenue statistics retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("monthly-ticket-type-statistics")]
        [SwaggerOperation(
            Summary = "Get monthly ticket type statistics",
            Description = "Retrieves the number of online, offline, and guest (non-member) tickets for a specified month and year."
        )]
        [ProducesResponseType(typeof(ApiResult<MonthlyTicketTypeStatisticDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetMonthlyTicketTypeStatisticsAsync([FromQuery] MonthYearDto monthYear)
        {
            try
            {
                if (monthYear.Month < 1 || monthYear.Month > 12)
                    return BadRequest(ApiResult<object>.Failure("400", "Month must be between 1 and 12."));
                if (monthYear.Year < 2000 || monthYear.Year > DateTime.UtcNow.Year)
                    return BadRequest(ApiResult<object>.Failure("400", "Year is out of valid range."));

                var result = await _statisticService.GetMonthlyTicketTypeStatisticsAsync(monthYear);
                return Ok(ApiResult<MonthlyTicketTypeStatisticDto>.Success(result, "200", "Monthly ticket type statistics retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("monthly-food-and-drink-revenue")]
        [SwaggerOperation(
            Summary = "Get monthly food and drink revenue statistics",
            Description = "Retrieves the total revenue and quantity sold for each food and drink item for a specified month and year."
        )]
        [ProducesResponseType(typeof(ApiResult<List<MonthlyFoodAndDrinkRevenueDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetMonthlyFoodAndDrinkRevenueAsync([FromQuery] MonthYearDto monthYear)
        {
            try
            {
                if (monthYear.Month < 1 || monthYear.Month > 12)
                    return BadRequest(ApiResult<object>.Failure("400", "Month must be between 1 and 12."));
                if (monthYear.Year < 2000 || monthYear.Year > DateTime.UtcNow.Year)
                    return BadRequest(ApiResult<object>.Failure("400", "Year is out of valid range."));

                var result = await _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);
                return Ok(ApiResult<List<MonthlyFoodAndDrinkRevenueDto>>.Success(result, "200", "Monthly food and drink revenue statistics retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}
