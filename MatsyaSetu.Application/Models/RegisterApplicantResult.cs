
namespace MatsyaSetu.Application.Models
{
    public record RegisterApplicantResult(
        bool IsSuccess,
        int? ApplicantId,
        string? Error);
}
