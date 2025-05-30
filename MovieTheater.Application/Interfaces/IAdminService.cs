using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        Task<bool> DeleteEmployeeAsync(Guid employeeId, Guid adminId);

    }
}