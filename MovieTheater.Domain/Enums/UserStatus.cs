namespace MovieTheater.Domain.Enums
{
    public enum UserStatus
    {
        Pending,        // User registered but not yet verified (e.g., email not confirmed)
        Active,         // User is active and can use the system
        Banned,         // User is permanently banned from the system
        Deleted         // User account is deleted (soft delete)
    }
}
