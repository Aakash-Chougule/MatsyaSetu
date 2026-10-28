
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
        private readonly IWhatsAppCustomerRepository _customerRepository;
        private readonly IBotMessageTemplateRepository _messageTemplateRepository;

        // ============================================================
        // CONSTRUCTOR - DEPENDENCY INJECTION
        // ============================================================

        public WhatsAppController(
            IConfiguration configuration,
            ILogger<WhatsAppController> logger,
            IWhatsAppService whatsAppService,
            IBotMenuRepository botMenuRepository,
            IBotActionDispatcher botActionDispatcher,
            IWhatsAppCustomerRepository customerRepository,
            IBotMessageTemplateRepository messageTemplateRepository)
        {
            _configuration = configuration;
            _logger = logger;
            _whatsAppService = whatsAppService;
            _botMenuRepository = botMenuRepository;
            _botActionDispatcher = botActionDispatcher;
            _customerRepository = customerRepository;
            _messageTemplateRepository = messageTemplateRepository;
        }

        // ============================================================
        // 1. VERIFY META WEBHOOK
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
                return Content(challenge, "text/plain");
            }

            return StatusCode(403);
        }

        // ============================================================
        // 2. RECEIVE WHATSAPP WEBHOOK
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook(
            CancellationToken cancellationToken)
        {
            string appSecret =
                _configuration["WhatsApp:AppSecret"]
                ?? throw new InvalidOperationException(
                    "WhatsApp AppSecret is missing.");

            string? signature =
                Request.Headers["X-Hub-Signature-256"]
                    .FirstOrDefault();

            // Validate signature header
            if (string.IsNullOrWhiteSpace(signature) ||
                !signature.StartsWith(
                    "sha256=",
                    StringComparison.Ordinal))
            {
                return Unauthorized();
            }

            // Read original request body
            using var stream = new MemoryStream();

            await Request.Body.CopyToAsync(
                stream,
                cancellationToken);

            byte[] payload = stream.ToArray();

            // Verify Meta HMAC SHA256 signature
            using var hmac = new HMACSHA256(
                Encoding.UTF8.GetBytes(appSecret));

            byte[] expectedHash = hmac.ComputeHash(payload);

            byte[] receivedHash;

            try
            {
                receivedHash =
                    Convert.FromHexString(signature[7..]);
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
            // 3. PROCESS WHATSAPP JSON
            // ========================================================

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(payload);

                JsonElement root = document.RootElement;

                if (GetString(root, "object") !=
                    "whatsapp_business_account")
                {
                    return BadRequest();
                }

                if (!root.TryGetProperty(
                        "entry",
                        out var entries) ||
                    entries.ValueKind != JsonValueKind.Array)
                {
                    return Ok();
                }

                foreach (var entry in entries.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object ||
                        !entry.TryGetProperty(
                            "changes",
                            out var changes) ||
                        changes.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var change in changes.EnumerateArray())
                    {
                        if (change.ValueKind != JsonValueKind.Object ||
                            GetString(change, "field") != "messages" ||
                            !change.TryGetProperty(
                                "value",
                                out var value) ||
                            value.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        // Validate receiving phone number ID
                        if (!value.TryGetProperty(
                                "metadata",
                                out var metadata))
                        {
                            continue;
                        }

                        string? receivingPhoneId =
                            GetString(metadata, "phone_number_id");

                        string? configuredPhoneId =
                            _configuration["WhatsApp:PhoneNumberId"];

                        if (string.IsNullOrWhiteSpace(configuredPhoneId) ||
                            receivingPhoneId != configuredPhoneId)
                        {
                            continue;
                        }

                        // Get incoming messages
                        if (!value.TryGetProperty(
                                "messages",
                                out var messages) ||
                            messages.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var message in messages.EnumerateArray())
                        {
                            if (message.ValueKind != JsonValueKind.Object)
                            {
                                continue;
                            }

                            string? senderWaId =
                                GetString(message, "from");

                            if (string.IsNullOrWhiteSpace(senderWaId))
                            {
                                continue;
                            }

                            // ============================================
                            // 4. DEVELOPMENT WHITELIST
                            // ============================================

                            string? allowedRecipient =
                                _configuration[
                                    "WhatsApp:AllowedTestRecipientWaId"];

                            if (string.IsNullOrWhiteSpace(allowedRecipient) ||
                                senderWaId != allowedRecipient)
                            {
                                continue;
                            }

                            string? messageType =
                                GetString(message, "type");

                            if (messageType != "text" &&
                                messageType != "interactive")
                            {
                                continue;
                            }

                            // ============================================
                            // 5. GET INCOMING TEXT
                            // ============================================

                            string? incomingText = null;

                            if (messageType == "text" &&
                                message.TryGetProperty(
                                    "text",
                                    out var textPayload))
                            {
                                incomingText =
                                    GetString(textPayload, "body")?.Trim();
                            }

                            // ============================================
                            // 6. CHECK CUSTOMER REGISTRATION
                            // ============================================

                            var customer = await _customerRepository
                                .GetByWhatsAppWaIdAsync(
                                    senderWaId,
                                    cancellationToken);

                            // Customer has not registered or consented
                            if (customer?.ConsentAt == null)
                            {
                                // ========================================
                                // 6.1 CUSTOMER DECLINES CONSENT
                                // ========================================

                                if (string.Equals(
                                        incomingText,
                                        "DECLINE",
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    var declinedTemplate =
                                        await _messageTemplateRepository
                                            .GetActiveByKeyAsync(
                                                "CONSENT_DECLINED",
                                                cancellationToken);

                                    if (declinedTemplate == null)
                                    {
                                        throw new InvalidOperationException(
                                            "CONSENT_DECLINED template is missing.");
                                    }

                                    await _whatsAppService
                                        .SendTextMessageAsync(
                                            senderWaId,
                                            declinedTemplate.MessageMarathi,
                                            cancellationToken);

                                    _logger.LogInformation(
                                        "Customer declined registration.");

                                    // Do not create customer record
                                    continue;
                                }

                                // Get consent request from SQL Server
                                var consentTemplate =
                                    await _messageTemplateRepository
                                        .GetActiveByKeyAsync(
                                            "CONSENT_REQUEST",
                                            cancellationToken);

                                if (consentTemplate == null)
                                {
                                    throw new InvalidOperationException(
                                        "CONSENT_REQUEST template is missing.");
                                }

                                // ========================================
                                // 6.2 CUSTOMER ACCEPTS CONSENT
                                // ========================================

                                if (string.Equals(
                                        incomingText,
                                        "AGREE",
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    // Register customer in SQL Server
                                    int customerId =
                                        await _customerRepository
                                            .CreateWithConsentAsync(
                                                senderWaId,
                                                DateTime.UtcNow,
                                                cancellationToken);

                                    // Confirm consent was saved
                                    var registeredCustomer =
                                        await _customerRepository
                                            .GetByWhatsAppWaIdAsync(
                                                senderWaId,
                                                cancellationToken);

                                    if (registeredCustomer?.ConsentAt == null)
                                    {
                                        throw new InvalidOperationException(
                                            "Customer consent could not be confirmed.");
                                    }

                                    _logger.LogInformation(
                                        "Customer {CustomerId} registered with consent.",
                                        customerId);

                                    // Fetch registration success message
                                    var acceptedTemplate =
                                        await _messageTemplateRepository
                                            .GetActiveByKeyAsync(
                                                "CONSENT_ACCEPTED",
                                                cancellationToken);

                                    if (acceptedTemplate == null)
                                    {
                                        throw new InvalidOperationException(
                                            "CONSENT_ACCEPTED template is missing.");
                                    }

                                    // Send Marathi confirmation
                                    await _whatsAppService
                                        .SendTextMessageAsync(
                                            senderWaId,
                                            acceptedTemplate.MessageMarathi,
                                            cancellationToken);

                                    // Show existing WhatsApp main menu
                                    await _whatsAppService
                                        .SendWelcomeMenuAsync(
                                            senderWaId,
                                            cancellationToken);

                                    continue;
                                }

                                // ========================================
                                // 6.3 NEW CUSTOMER - REQUEST CONSENT
                                // ========================================

                                await _whatsAppService
                                    .SendTextMessageAsync(
                                        senderWaId,
                                        consentTemplate.MessageMarathi,
                                        cancellationToken);

                                // Do not allow menu access without consent
                                continue;
                            }

                            // ============================================
                            // 7. EXISTING CUSTOMER
                            // ============================================

                            // Update last activity in SQL Server
                            await _customerRepository
                                .UpdateLastSeenAsync(
                                    customer.CustomerId,
                                    cancellationToken);

                            // ============================================
                            // 8. HANDLE NORMAL TEXT
                            // ============================================

                            if (messageType == "text")
                            {
                                if (!message.TryGetProperty(
                                        "text",
                                        out var text))
                                {
                                    continue;
                                }

                                string body =
                                    GetString(text, "body")?.Trim() ?? "";

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
                                            senderWaId,
                                            cancellationToken);

                                    _logger.LogInformation(
                                        "Welcome menu submitted.");
                                }

                                continue;
                            }

                            // ============================================
                            // 9. HANDLE INTERACTIVE MENU SELECTION
                            // ============================================

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
                                        "type") != "list_reply")
                                {
                                    continue;
                                }

                                if (!interactive.TryGetProperty(
                                        "list_reply",
                                        out var listReply))
                                {
                                    continue;
                                }

                                string? selectionId =
                                    GetString(listReply, "id");

                                if (string.IsNullOrWhiteSpace(selectionId))
                                {
                                    continue;
                                }

                                _logger.LogInformation(
                                    "WhatsApp option selected: {Option}",
                                    selectionId);

                                // ========================================
                                // 10. GET SELECTED MENU FROM SQL SERVER
                                // ========================================

                                var selectedMenuItem =
                                    await _botMenuRepository
                                        .GetByOptionKeyAsync(
                                            selectionId,
                                            cancellationToken);

                                if (selectedMenuItem == null)
                                {
                                    _logger.LogWarning(
                                        "Menu option not found: {Option}",
                                        selectionId);

                                    continue;
                                }

                                // ========================================
                                // 11. HANDLE SUBMENU
                                // ========================================

                                if (string.Equals(
                                        selectedMenuItem.ResponseType,
                                        "SUBMENU",
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.LogInformation(
                                        "Opening submenu: {MenuItemId}",
                                        selectedMenuItem.MenuItemId);

                                    await _whatsAppService
                                        .SendMenuAsync(
                                            senderWaId,
                                            cancellationToken,
                                            selectedMenuItem.MenuItemId);

                                    continue;
                                }

                                // ========================================
                                // 12. HANDLE DOCUMENT / PDF
                                // ========================================

                                if (string.Equals(
                                        selectedMenuItem.ResponseType,
                                        "DOCUMENT",
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.LogInformation(
                                        "Document selected: {MenuItemId}",
                                        selectedMenuItem.MenuItemId);

                                    // Check document URL before responding
                                    if (string.IsNullOrWhiteSpace(
                                            selectedMenuItem.DocumentUrl))
                                    {
                                        _logger.LogWarning(
                                            "Document URL missing: {MenuItemId}",
                                            selectedMenuItem.MenuItemId);

                                        continue;
                                    }

                                    // Send Marathi message first
                                    if (!string.IsNullOrWhiteSpace(
                                            selectedMenuItem.ResponseMessageMarathi))
                                    {
                                        await _whatsAppService
                                            .SendTextMessageAsync(
                                                senderWaId,
                                                selectedMenuItem.ResponseMessageMarathi,
                                                cancellationToken);
                                    }

                                    // Send document stored at SQL URL
                                    await _whatsAppService
                                        .SendDocumentMessageAsync(
                                            senderWaId,
                                            selectedMenuItem.DocumentUrl,
                                            selectedMenuItem.DocumentFileName,
                                            null,
                                            cancellationToken);

                                    _logger.LogInformation(
                                        "Document submitted: {MenuItemId}",
                                        selectedMenuItem.MenuItemId);

                                    continue;
                                }

                                // ========================================
                                // 13. HANDLE ACTION CODE
                                // ========================================

                                string actionCode =
                                    selectedMenuItem.ActionCode;

                                var context = new BotActionContext
                                {
                                    SenderWaId = senderWaId,
                                    OptionKey = selectionId
                                };

                                string? reply =
                                    await _botActionDispatcher
                                        .DispatchAsync(
                                            actionCode,
                                            context,
                                            cancellationToken);

                                if (string.IsNullOrWhiteSpace(reply))
                                {
                                    _logger.LogWarning(
                                        "No handler found for ActionCode: {ActionCode}",
                                        actionCode);

                                    continue;
                                }

                                // Send handler response
                                await _whatsAppService
                                    .SendTextMessageAsync(
                                        senderWaId,
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
        // HELPER - READ STRING PROPERTY FROM JSON
        // ============================================================

        private static string? GetString(
            JsonElement parent,
            string propertyName)
        {
            if (parent.ValueKind != JsonValueKind.Object ||
                !parent.TryGetProperty(
                    propertyName,
                    out var element) ||
                element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return element.GetString();
        }
    }
}
