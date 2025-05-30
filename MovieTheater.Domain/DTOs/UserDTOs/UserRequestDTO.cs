using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.UserDTOs
{
    public class UserRequestDTO
    {
        [StringLength(255, ErrorMessage = "Image URL cannot exceed 255 characters.")]
        [DefaultValue(null)]
        public string? Image { get; set; }  // Chuỗi lưu đường dẫn ảnh hoặc base64

        [Required(ErrorMessage = "Employee Name is required.")]
        [StringLength(28, ErrorMessage = "Employee Name cannot exceed 28 characters.")]
        [DefaultValue("Default Name")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DefaultValue(typeof(DateTime), "2000-01-01")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Sex is required.")]
        [DefaultValue(Gender.Male)]
        public Gender Sex { get; set; }

        [Required(ErrorMessage = "Identity Card (CCCD) is required.")]
        [StringLength(28, ErrorMessage = "Identity Card cannot exceed 28 characters.")]
        [DefaultValue("000000000")]
        public required string IdentityCard { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(28, ErrorMessage = "Email cannot exceed 28 characters.")]
        [DefaultValue("default@example.com")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0.")]
        [StringLength(28)]
        [DefaultValue("0123456789")]
        public required string PhoneNumber { get; set; }

        [StringLength(28, ErrorMessage = "Address cannot exceed 28 characters.")]
        [DefaultValue("Default Address")]
        public string? Address { get; set; }
    }
}
