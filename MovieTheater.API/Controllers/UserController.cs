using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IClaimsService _claimsService;
        private readonly ILoggerService _loggerService;
        public UserController(IUserService userService, IClaimsService claimsService, ILoggerService loggerService)
        {
            _userService = userService;
            _claimsService = claimsService;
            _loggerService = loggerService;
        }
    }
}
