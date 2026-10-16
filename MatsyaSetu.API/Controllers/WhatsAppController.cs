using Microsoft.AspNetCore.Mvc;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MatsyaSetu.API.Controllers
{
    [ApiController]
    [Route("api/whatsapp/webhook")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppController> _logger;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IBotMenuRepository _botMenuRepository;
        private readonly IBotActionDispatcher _botActionDispatcher;

        // ============================================================
        // Constructor Dependency Injection
        // ============================================================

        public WhatsAppController(
            IConfiguration configuration,
            ILogger<WhatsAppController> logger,
            IWhatsAppService whatsAppService,
            IBotMenuRepository botMenuRepository,
            IBotActionDispatcher botActionDispatcher)
        {
            _configuration = configuration;
            _logger = logger;
            _whatsAppService = whatsAppService;
            _botMenuRepository = botMenuRepository;
            _botActionDispatcher = botActionDispatcher;
        }

        // ============================================================
        // 1. GET - Meta webhook verification
        // ============================================================

        [HttpGet]
        public IActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? token,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            string? verifyToken =
                _configuration["WhatsApp:VerifyToken"];

            if (mode == "subscribe" &&
                !string.IsNullOrWhiteSpace(verifyToken) &&
                string.Equals(
                    token,
                    verifyToken,
                    StringComparison.Ordinal) &&
                challenge != null)
            {
                return Content(
                    challenge,
                    "text/plain");
            }

            return StatusCode(403);
        }

        // ============================================================
        // 2. POST - Receive WhatsApp webhook events
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook(
            CancellationToken cancellationToken)
        {
            // --------------------------------------------------------
            // Read Meta App Secret
            // --------------------------------------------------------

            string appSecret =
                _configuration["WhatsApp:AppSecret"]
                ?? throw new InvalidOperationException(
                    "WhatsApp AppSecret is missing.");

            // --------------------------------------------------------
            // Signature supplied by Meta
            // --------------------------------------------------------

            string? signature =
                Request.Headers["X-Hub-Signature-256"]
                    .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(signature) ||
                !signature.StartsWith(
                    "sha256=",
                    StringComparison.Ordinal))
            {
                return Unauthorized();
            }

            // --------------------------------------------------------
            // Read original webhook body
            // --------------------------------------------------------

            using var stream =
                new MemoryStream();

            await Request.Body.CopyToAsync(
                stream,
                cancellationToken);

            byte[] payload =
                stream.ToArray();

            // --------------------------------------------------------
            // Verify HMAC SHA256 signature
            // --------------------------------------------------------

            using var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(
                        appSecret));

            byte[] expectedHash =
                hmac.ComputeHash(payload);

            byte[] receivedHash;

            try
            {
                receivedHash =
                    Convert.FromHexString(
                        signature[7..]);
            }
            catch (FormatException)
            {
                return Unauthorized();
            }

            if (!CryptographicOperations.FixedTimeEquals(
                    expectedHash,
                    receivedHash))
            {
                _logger.LogWarning(
                    "Invalid WhatsApp webhook signature.");

                return Unauthorized();
            }

            _logger.LogInformation(
                "Verified WhatsApp webhook received.");

            // ========================================================
            // 3. Process WhatsApp JSON
            // ========================================================

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(payload);

                JsonElement root =
                    document.RootElement;

                // ----------------------------------------------------
                // Check webhook object
                // ----------------------------------------------------

                if (GetString(root, "object") !=
                    "whatsapp_business_account")
                {
                    return BadRequest();
                }

                // ----------------------------------------------------
                // Check entry array
                // ----------------------------------------------------

                if (!root.TryGetProperty(
                        "entry",
                        out var entries) ||
                    entries.ValueKind !=
                    JsonValueKind.Array)
                {
                    return Ok();
                }

                foreach (var entry
                    in entries.EnumerateArray())
                {
                    if (entry.ValueKind !=
                            JsonValueKind.Object ||
                        !entry.TryGetProperty(
                            "changes",
                            out var changes) ||
                        changes.ValueKind !=
                            JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var change
                        in changes.EnumerateArray())
                    {
                        // ------------------------------------------------
                        // We only process message events
                        // ------------------------------------------------

                        if (change.ValueKind !=
                                JsonValueKind.Object ||
                            GetString(
                                change,
                                "field") != "messages" ||
                            !change.TryGetProperty(
                                "value",
                                out var value) ||
                            value.ValueKind !=
                                JsonValueKind.Object)
                        {
                            continue;
                        }

                        // ------------------------------------------------
                        // Validate receiving WhatsApp number
                        // ------------------------------------------------

                        if (!value.TryGetProperty(
                                "metadata",
                                out var metadata))
                        {
                            continue;
                        }

                        string? receivingPhoneId =
                            GetString(
                                metadata,
                                "phone_number_id");

                        string? configuredPhoneId =
                            _configuration[
                                "WhatsApp:PhoneNumberId"];

                        if (string.IsNullOrWhiteSpace(
                                configuredPhoneId) ||
                            receivingPhoneId !=
                                configuredPhoneId)
                        {
                            continue;
                        }

                        // ------------------------------------------------
                        // Get incoming messages
                        // ------------------------------------------------

                        if (!value.TryGetProperty(
                                "messages",
                                out var messages) ||
                            messages.ValueKind !=
                                JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var message
                            in messages.EnumerateArray())
                        {
                            if (message.ValueKind !=
                                JsonValueKind.Object)
                            {
                                continue;
                            }

                            string? senderWaId =
                                GetString(
                                    message,
                                    "from");

                            // ------------------------------------------------
                            // Development whitelist
                            // ------------------------------------------------

                            string? allowedRecipient =
                                _configuration[
                                    "WhatsApp:AllowedTestRecipientWaId"];

                            if (string.IsNullOrWhiteSpace(
                                    allowedRecipient) ||
                                senderWaId !=
                                    allowedRecipient)
                            {
                                continue;
                            }

                            string? messageType =
                                GetString(
                                    message,
                                    "type");

                            // =================================================
                            // 4. Handle normal text
                            // =================================================

                            if (messageType == "text")
                            {
                                if (!message.TryGetProperty(
                                        "text",
                                        out var text))
                                {
                                    continue;
                                }

                                string body =
                                    GetString(
                                        text,
                                        "body")?
                                    .Trim() ?? "";

                                bool isWelcome =
                                    body.Equals(
                                        "Hi",
                                        StringComparison.OrdinalIgnoreCase)
                                    ||
                                    body.Equals(
                                        "Hello",
                                        StringComparison.OrdinalIgnoreCase)
                                    ||
                                    body.Equals(
                                        "नमस्कार",
                                        StringComparison.Ordinal);

                                if (isWelcome)
                                {
                                    await _whatsAppService
                                        .SendWelcomeMenuAsync(
                                            senderWaId!,
                                            cancellationToken);

                                    _logger.LogInformation(
                                        "Welcome menu submitted.");
                                }

                                continue;
                            }

                            // =================================================
                            // 5. Handle interactive WhatsApp list selection
                            // =================================================

                            if (messageType == "interactive")
                            {
                                if (!message.TryGetProperty(
                                        "interactive",
                                        out var interactive))
                                {
                                    continue;
                                }

                                if (GetString(
                                        interactive,
                                        "type") !=
                                    "list_reply")
                                {
                                    continue;
                                }

                                if (!interactive.TryGetProperty(
                                        "list_reply",
                                        out var listReply))
                                {
                                    continue;
                                }

                                // Example:
                                // scheme_info

                                string? selectionId =
                                    GetString(
                                        listReply,
                                        "id");

                                if (string.IsNullOrWhiteSpace(
                                        selectionId))
                                {
                                    continue;
                                }

                                _logger.LogInformation(
                                    "WhatsApp option selected: {Option}",
                                    selectionId);

                                // ============================================
                                // 6. Get selected option FROM DATABASE
                                // ============================================

                                var selectedMenuItem =
                                    await _botMenuRepository
                                        .GetByOptionKeyAsync(
                                            selectionId,
                                            cancellationToken);

                                // Option does not exist / disabled
                                if (selectedMenuItem == null)
                                {
                                    _logger.LogWarning(
                                        "Menu option not found in database: {Option}",
                                        selectionId);

                                    continue;
                                }

                                string actionCode =
                                    selectedMenuItem.ActionCode;

                                _logger.LogInformation(
                                    "Database ActionCode: {ActionCode}",
                                    actionCode);

                                // ============================================
                                // 7. Dispatch action to appropriate handler
                                // ============================================

                                var context =
                                    new BotActionContext
                                    {
                                        SenderWaId = senderWaId!,
                                        OptionKey = selectionId
                                    };

                                string? reply =
                                    await _botActionDispatcher
                                        .DispatchAsync(
                                            actionCode,
                                            context,
                                            cancellationToken);

                                // ============================================
                                // Unknown ActionCode
                                // ============================================

                                if (string.IsNullOrWhiteSpace(
                                        reply))
                                {
                                    _logger.LogWarning(
                                        "No handler found for ActionCode: {ActionCode}",
                                        actionCode);

                                    continue;
                                }

                                // ============================================
                                // Send response
                                // ============================================

                                await _whatsAppService
                                    .SendTextMessageAsync(
                                        senderWaId!,
                                        reply,
                                        cancellationToken);

                                _logger.LogInformation(
                                    "Response sent for ActionCode: {ActionCode}",
                                    actionCode);
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid WhatsApp webhook JSON.");

                return BadRequest();
            }
        }

        // ============================================================
        // Helper method
        // ============================================================

        private static string? GetString(JsonElement parent, string propertyName)
        {
            if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return element.GetString();
        }
    }
}