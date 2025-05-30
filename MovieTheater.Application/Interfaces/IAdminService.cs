using MovieTheater.Domain.DTOs.AdminDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;

namespace MovieTheater.Application.Interfaces
{
    public interface IAdminService
    {
        Task<Pagination<GetUserDto>> GetListUsersAsync(
            string? search,
            RoleType? role,
            string? sortBy,
            bool isDescending,
            int page,
            int pageSize);

        Task<Pagination<UserDto>> GetAllEmployeesAsync(
             string? search,
             string? sortBy,
             bool isDescending,
             int page,
            int pageSize
            );

        Task<UserDto?> AddEmployeeAsync(AddEmployeeRequestDto dto);
        Task<bool> DeleteEmployeeAsync(Guid employeeId, Guid adminId);

    }
}