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

        // Constructor Dependency Injection
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
            // Read Meta credentials from configuration/User Secrets
            string accessToken =
                _configuration["WhatsApp:AccessToken"]
                ?? throw new InvalidOperationException(
                    "WhatsApp AccessToken is missing.");

            string phoneNumberId =
                _configuration["WhatsApp:PhoneNumberId"]
                ?? throw new InvalidOperationException(
                    "WhatsApp PhoneNumberId is missing.");

            // Meta WhatsApp Cloud API endpoint
            string url =
                $"https://graph.facebook.com/v26.0/" +
                $"{phoneNumberId}/messages";

            // Create normal text message
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

            // Create HTTP request
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url);

            // Add Meta Access Token
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            // Convert object into JSON
            request.Content =
                JsonContent.Create(payload);

            // Send request to Meta
            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            // Read Meta response
            string responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            // If Meta returns error
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Meta API returned HTTP " +
                    $"{(int)response.StatusCode}. " +
                    $"Response: {responseBody}");
            }
        }

        // ============================================================
        // Send WhatsApp interactive welcome menu
        // ============================================================
        public async Task SendWelcomeMenuAsync(
            string recipientWaId,
            CancellationToken cancellationToken = default)
        {
            // --------------------------------------------------------
            // 1. Read menu options from SQL Server
            // --------------------------------------------------------

            var menuItems =
                await _botMenuRepository
                    .GetActiveMenuItemsAsync(
                        cancellationToken);

            // Don't send an empty WhatsApp menu
            if (menuItems == null ||
                menuItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "No active WhatsApp menu items found in database.");
            }

            // --------------------------------------------------------
            // 2. Read Meta credentials
            // --------------------------------------------------------

            string accessToken =
                _configuration["WhatsApp:AccessToken"]
                ?? throw new InvalidOperationException(
                    "WhatsApp AccessToken is missing.");

            string phoneNumberId =
                _configuration["WhatsApp:PhoneNumberId"]
                ?? throw new InvalidOperationException(
                    "WhatsApp PhoneNumberId is missing.");

            // Meta WhatsApp Cloud API endpoint
            string url =
                $"https://graph.facebook.com/v26.0/" +
                $"{phoneNumberId}/messages";

            // --------------------------------------------------------
            // 3. Create dynamic WhatsApp interactive menu
            // --------------------------------------------------------

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

                                // ==================================
                                // MENU NOW COMES FROM DATABASE
                                // ==================================
                                rows = menuItems
                                    .Take(10)
                                    .Select(item => new
                                    {
                                        // Example:
                                        // scheme_info
                                        id = item.OptionKey,

                                        // Example:
                                        // योजनेची माहिती
                                        title = item.MarathiTitle
                                    })
                                    .ToArray()
                            }
                        }
                    }
                }
            };

            // --------------------------------------------------------
            // 4. Create HTTP request
            // --------------------------------------------------------

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url);

            // Add Meta Access Token
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            // Convert payload into JSON
            request.Content =
                JsonContent.Create(payload);

            // --------------------------------------------------------
            // 5. Send request to Meta
            // --------------------------------------------------------

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            // Read Meta response
            string responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            // --------------------------------------------------------
            // 6. Handle Meta error
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