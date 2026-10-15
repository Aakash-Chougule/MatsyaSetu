
namespace MatsyaSetu.Domain.Entities
{
    public class Applicant
    {
        // Unique ID of the applicant
        public int ApplicantId { get; set; }

        // Applicant's full name
        public string FullName { get; set; } = string.Empty;

        // Registered mobile number
        public string MobileNumber { get; set; } = string.Empty;

        // Indicates successful OTP verification
        public bool IsMobileVerified { get; set; }

        // Applicant registration date and time
        public DateTime CreatedAt { get; set; }
    }
}
