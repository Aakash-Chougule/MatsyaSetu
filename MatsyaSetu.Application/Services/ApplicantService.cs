
using System.Text.RegularExpressions;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;
using MatsyaSetu.Domain.Entities;

namespace MatsyaSetu.Application.Services
{
    public class ApplicantService : IApplicantService
    {
        private readonly IApplicantRepository _repository;

        // Constructor Dependency Injection
        public ApplicantService(
            IApplicantRepository repository)
        {
            _repository = repository;
        }

        // Validate Indian mobile-number format
        public bool HasValidIndianMobileFormat(
            string? mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
                return false;

            return Regex.IsMatch(
                mobileNumber,
                @"^[6-9][0-9]{9}$");
        }

        // Register an applicant
        public async Task<RegisterApplicantResult> RegisterAsync(
            string fullName,
            string mobileNumber,
            CancellationToken cancellationToken = default)
        {
            // Step 1: Validate the applicant's name
            fullName = fullName?.Trim() ?? string.Empty;

            if (fullName.Length < 2 || fullName.Length > 150)
            {
                return new RegisterApplicantResult(
                    false, null, "Invalid applicant name.");
            }

            // Step 2: Validate mobile-number format
            if (!HasValidIndianMobileFormat(mobileNumber))
            {
                return new RegisterApplicantResult(
                    false, null, "Invalid mobile number.");
            }

            // Step 3: Create our Domain entity
            var applicant = new Applicant
            {
                FullName = fullName,
                MobileNumber = mobileNumber,
                IsMobileVerified = false
            };

            // Step 4: Insert applicant using repository
            int applicantId =
                await _repository.CreateAsync(
                    applicant, cancellationToken);

            // Step 5: Return generated ID
            return new RegisterApplicantResult(
                true, applicantId, null);
        }
    }
}
