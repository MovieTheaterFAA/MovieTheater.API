using MovieTheater.Domain.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.DTOs.AdminDTOs
{
    public class AddEmployeeRequestDto
    {
        [Required(ErrorMessage = "Employee Name is required.")]
        [StringLength(28, ErrorMessage = "Employee Name cannot exceed 28 characters.")]
        [DefaultValue("Default Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DefaultValue(typeof(DateTime), "2000-01-01")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Sex is required.")]
        [DefaultValue(Gender.Male)]
        public Gender Sex { get; set; }

        [Required(ErrorMessage = "Identity Card (CCCD) is required.")]
        [StringLength(28, ErrorMessage = "Identity Card cannot exceed 28 characters.")]
        [DefaultValue("000000000")]
        public string CCCD { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 28 characters.")]
        [DefaultValue("default@example.com")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0.")]
        [StringLength(28)]
        [DefaultValue("0123456789")]
        public string PhoneNumber { get; set; }

        [StringLength(300, ErrorMessage = "Address cannot exceed 28 characters.")]
        [DefaultValue("Default Address")]
        public string? Address { get; set; }

        public DateTime CreateAt { get; set; }
    }
}