using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.Enums
{
    public enum UserStatus
    {
        Pending,        // User registered but not yet verified (e.g., email not confirmed)
        Active,         // User is active and can use the system
        Inactive,       // User account is temporarily disabled (e.g., by admin)
        Banned,         // User is permanently banned from the system
        Deleted         // User account is deleted (soft delete)
    }
}
