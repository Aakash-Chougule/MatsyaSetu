
using MatsyaSetu.Domain.Entities;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IApplicantRepository
    {
        // Insert an applicant and return the generated ID
        Task<int> CreateAsync(
            Applicant applicant,
            CancellationToken cancellationToken = default);
    }
}
