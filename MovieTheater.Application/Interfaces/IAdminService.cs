using MovieTheater.Domain.DTOs.AdminDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;

namespace MovieTheater.Application.Interfaces
{
    public interface IAdminService
    {
        Task<Pagination<GetUserDto>> GetListUserAsync(
            string? search,
            RoleType? role,
            string? sortBy,
            bool isDescending,
            int page,
            int pageSize);

        Task<Pagination<UserDto>> GetListEmployeeAsync(
             string? search,
             string? sortBy,
             bool isDescending,
             int page,
            int pageSize
            );

        Task<UserDto?> AddEmployeeAsync(AddEmployeeRequestDto dto);

        Task<EditEmployeeDto> EditEmployeeAsync(Guid userId, EditEmployeeDto editEmployeeDto);

        Task<bool> DeleteEmployeeAsync(Guid employeeId, Guid adminId);
        Task<bool> BanUserAsync(Guid userId, Guid adminId);
    }
}