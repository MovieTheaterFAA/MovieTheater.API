using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.DTOs.AuthenDTOs;

public class UserRegistrationDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [DefaultValue("ch1mple@gmail.com")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [DefaultValue("Ch1mple@")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    [DefaultValue("Ch1mple")]
    public required string FullName { get; set; }

    [DefaultValue("2002-03-06T00:00:00Z")] 
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number.")]
    [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0.")]
    [DefaultValue("0393734206")]
    public required string PhoneNumber { get; set; }
}