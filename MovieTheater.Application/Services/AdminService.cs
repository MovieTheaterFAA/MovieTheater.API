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
    public async Task<List<UserDto>> GetAllEmloyees()
    {
        try
        {
            _loggerService.Info("Starting to retrieve all employees.");

            var listusers = await _unitOfWork.Users.GetAllAsync();

            if (listusers == null || !listusers.Any(u => u.Role == RoleType.Employee))
            {
                _loggerService.Warn("No employee users found.");               
            }

            var result = listusers
                .Where(u => u.Role == RoleType.Employee)
                .Select(user => new UserDto
                {
                    FullName = user.FullName,
                    CCCD = user.CCCD,
                    DateOfBirth = user.DateOfBirth,
                    Sex = user.Sex,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Role = user.Role
                }).ToList();

            _loggerService.Success($"Retrieved {result.Count} employee(s) successfully.");
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Failed to retrieve employees. Exception: {ex.Message}");
            throw;
        }
    }
}