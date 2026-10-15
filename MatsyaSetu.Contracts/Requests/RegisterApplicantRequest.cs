
using System.ComponentModel.DataAnnotations;

namespace MatsyaSetu.Contracts.Requests
{
    public class RegisterApplicantRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string FullName { get; set; } =
            string.Empty;

        [Required]
        public string MobileNumber { get; set; } =
            string.Empty;
    }
}
