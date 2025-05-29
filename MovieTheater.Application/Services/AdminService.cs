using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Application.Services;

public class AdminService : IAdminService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(IUnitOfWork unitOfWork, ILoggerService loggerService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
    }

    public async Task<Pagination<GetUserDto>> GetListUsersAsync(string? search, RoleType? role, string? sortBy, bool isDescending, int page, int pageSize)
    {
        try
        {
            _loggerService.Info($"Fetching users - Page {page}, PageSize {pageSize}, Role: {role}, Search: {search}");

            var query = _unitOfWork.Users.GetQueryable();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(searchLower) || u.Email.ToLower().Contains(searchLower));
            }

            var totalUsers = await query.CountAsync();

            if (!string.IsNullOrEmpty(sortBy))
            {
                query = sortBy switch
                {
                    "ScoreBalance" => isDescending ? query.OrderByDescending(u => u.ScoreBalance) : query.OrderBy(u => u.ScoreBalance),
                    "CreatedAt" => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                    _ => query.OrderBy(u => u.Id)
                };
            }
            else
            {
                query = query.OrderBy(u => u.Id);
            }

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!users.Any())
            {
                _loggerService.Warn($"No user found on page {page}");
                return new Pagination<GetUserDto>(new List<GetUserDto>(), 0, page, pageSize);
            }

            var userDtos = users.Select(u => new GetUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Sex = u.Sex,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                CCCD = u.CCCD,
                Address = u.Address,
                Role = u.Role,
                ScoreBalance = u.ScoreBalance,
                CreatedAt = u.CreatedAt,
                AvatarUrl = u.AvatarUrl,
            }).ToList();

            _loggerService.Success($"Retrieved {userDtos.Count} users on page {page}");

            return new Pagination<GetUserDto>(userDtos, totalUsers, page, pageSize);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error while fetching users: {ex.Message}");
            throw new Exception("An error occurred while fetching users. Please try again later");
        }
    }

    public async Task<Pagination<UserDto>> GetAllEmployeesAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize)
    {
        try
        {
            _loggerService.Info($"Fetching employees - Page {page}, PageSize {pageSize}, Search: {search}");

            var listUsers = await _unitOfWork.Users.GetAllAsync();

            var employeeUsers = listUsers.Where(u => u.Role == RoleType.Employee).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                employeeUsers = employeeUsers.Where(u =>
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.ToLower().Contains(searchLower))
                );
            }

            var totalEmployees = employeeUsers.Count();
          
            employeeUsers = sortBy?.ToLower() switch
            {
                "fullname" => isDescending ? employeeUsers.OrderByDescending(u => u.FullName) : employeeUsers.OrderBy(u => u.FullName),
                "dateofbirth" => isDescending ? employeeUsers.OrderByDescending(u => u.DateOfBirth) : employeeUsers.OrderBy(u => u.DateOfBirth),
                _ => employeeUsers.OrderBy(u => u.Id)
            };


            var pagedEmployees = employeeUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = pagedEmployees.Select(user => new UserDto
            {
                AvatarUrl = user.AvatarUrl,
                FullName = user.FullName,
                CCCD = user.CCCD,
                DateOfBirth = user.DateOfBirth,
                Sex = user.Sex,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            }).ToList();

            _loggerService.Success($"Retrieved {result.Count} employees on page {page} successfully.");

            return new Pagination<UserDto>(result, totalEmployees, page, pageSize);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Failed to retrieve employees. Exception: {ex.Message}");
            throw new Exception("An error occurred while retrieving employees. Please try again later.");
        }
    }
}