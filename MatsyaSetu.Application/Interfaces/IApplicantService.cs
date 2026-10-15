
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IApplicantService
    {
        // Check the basic mobile-number format
        bool HasValidIndianMobileFormat(
            string? mobileNumber);

        // Register an applicant as unverified
        Task<RegisterApplicantResult> RegisterAsync(
            string fullName,
            string mobileNumber,
            CancellationToken cancellationToken = default);
    }
}
