namespace MovieTheater.Domain.Enums
{
    public enum UserStatus
    {
        Pending = 0,        // User registered but not yet verified (e.g., email not confirmed)
        Active = 1,         // User is active and can use the system
        Banne = 2,         // User is permanently banned from the system
        Deleted = 3,       // User account is deleted (soft delete)
    }
}