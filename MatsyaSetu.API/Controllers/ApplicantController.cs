
using Microsoft.AspNetCore.Mvc;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Contracts.Requests;

namespace MatsyaSetu.API.Controllers
{
    [ApiController]
    [Route("api/applicants")]
    public class ApplicantController : ControllerBase
    {
        // Interface used instead of a concrete class
        private readonly IApplicantService _applicantService;

        // ASP.NET Core injects the registered service
        public ApplicantController(
            IApplicantService applicantService)
        {
            _applicantService = applicantService;
        }

        [HttpPost("validate-mobile")]
        public IActionResult ValidateMobile(
            [FromBody] ValidateMobileRequest request)
        {
            // Call the application layer
            bool isValid =
                _applicantService.HasValidIndianMobileFormat(
                    request.MobileNumber);

            // Return the validation result
            return Ok(new
            {
                isValidFormat = isValid,
                message = isValid
                    ? "Valid mobile number format."
                    : "Invalid mobile number format."
            });
        }

        // Register an applicant
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterApplicantRequest request,
            CancellationToken cancellationToken)
        {
            // Call the Application layer
            var result = await _applicantService.RegisterAsync(
                request.FullName,
                request.MobileNumber,
                cancellationToken);

            // Return HTTP 400 if validation fails
            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    message = result.Error
                });
            }

            // Return HTTP 201 after successful insertion
            return StatusCode(201, new
            {
                message = "Applicant registered as unverified.",
                applicantId = result.ApplicantId
            });
        }

    }
}
