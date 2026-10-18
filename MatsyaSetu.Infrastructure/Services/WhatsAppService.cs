using System.Net.Http.Headers;
using System.Net.Http.Json;
using MatsyaSetu.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MatsyaSetu.Infrastructure.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IBotMenuRepository _botMenuRepository;

        public WhatsAppService(
            HttpClient httpClient,
            IConfiguration configuration,
            IBotMenuRepository botMenuRepository)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _botMenuRepository = botMenuRepository;
        }

        // ============================================================
        // Send normal WhatsApp text message
        // ============================================================
        public async Task SendTextMessageAsync(
            string recipientWaId,
            string message,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                messaging_product = "whatsapp",

                recipient_type = "individual",

                to = recipientWaId,

                type = "text",

                text = new
                {
                    preview_url = false,
                    body = message
                }
            };

            await SendMetaMessageAsync(
                payload,
                cancellationToken);
        }

        // ============================================================
        // Send WhatsApp interactive welcome menu
        // ============================================================
        public async Task SendWelcomeMenuAsync(
            string recipientWaId,
            CancellationToken cancellationToken = default)
        {
            // --------------------------------------------------------
            // 1. Get active menu items from database
            // --------------------------------------------------------

            var menuItems =
                await _botMenuRepository
                    .GetActiveMenuItemsAsync(
                        cancellationToken);

            if (menuItems == null ||
                menuItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "No active WhatsApp menu items found in database.");
            }

            // --------------------------------------------------------
            // 2. Build dynamic menu payload
            // --------------------------------------------------------

            var rows = menuItems
                .Take(10)
                .Select(item => new
                {
                    id = item.OptionKey,
                    title = item.MarathiTitle
                })
                .ToArray();

            var payload = new
            {
                messaging_product = "whatsapp",

                recipient_type = "individual",

                to = recipientWaId,

                type = "interactive",

                interactive = new
                {
                    type = "list",

                    body = new
                    {
                        text =
                            "नमस्कार! 🙏\n" +
                            "मत्स्यसेतू सेवेमध्ये आपले स्वागत आहे.\n\n" +
                            "कृपया खालील पर्यायांपैकी एक निवडा."
                    },

                    footer = new
                    {
                        text = "ही चाचणी सेवा आहे."
                    },

                    action = new
                    {
                        button = "सेवा निवडा",

                        sections = new[]
                        {
                            new
                            {
                                title = "मुख्य सेवा",
                                rows = rows
                            }
                        }
                    }
                }
            };

            // --------------------------------------------------------
            // 3. Send through common Meta API method
            // --------------------------------------------------------

            await SendMetaMessageAsync(
                payload,
                cancellationToken);
        }

        // ============================================================
        // Common Meta WhatsApp API method
        // ============================================================
        private async Task SendMetaMessageAsync(
            object payload,
            CancellationToken cancellationToken)
        {
            // --------------------------------------------------------
            // Read Meta configuration
            // --------------------------------------------------------

            string accessToken =
                _configuration["WhatsApp:AccessToken"]
                ?? throw new InvalidOperationException(
                    "WhatsApp AccessToken is missing.");

            string phoneNumberId =
                _configuration["WhatsApp:PhoneNumberId"]
                ?? throw new InvalidOperationException(
                    "WhatsApp PhoneNumberId is missing.");

            // --------------------------------------------------------
            // Build Meta API endpoint
            // --------------------------------------------------------

            string url =
                $"https://graph.facebook.com/v26.0/" +
                $"{phoneNumberId}/messages";

            // --------------------------------------------------------
            // Create HTTP request
            // --------------------------------------------------------

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url);

            // --------------------------------------------------------
            // Add authentication
            // --------------------------------------------------------

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            // --------------------------------------------------------
            // Add JSON body
            // --------------------------------------------------------

            request.Content =
                JsonContent.Create(payload);

            // --------------------------------------------------------
            // Send request
            // --------------------------------------------------------

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            // --------------------------------------------------------
            // Read Meta response
            // --------------------------------------------------------

            string responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            // --------------------------------------------------------
            // Handle Meta API error
            // --------------------------------------------------------

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Meta API returned HTTP " +
                    $"{(int)response.StatusCode}. " +
                    $"Response: {responseBody}");
            }
        }
    }
}