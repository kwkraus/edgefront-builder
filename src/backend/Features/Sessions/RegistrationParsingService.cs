using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using EdgeFront.Builder.Features.Sessions.Dtos;

namespace EdgeFront.Builder.Features.Sessions;

/// <summary>
/// Service for parsing registration CSV text using Azure AI Foundry Projects API.
/// Sends CSV content to the AI model with a structured prompt to extract registrant information.
/// Uses EntraID (DefaultAzureCredential) for authentication.
/// </summary>
public class RegistrationParsingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly TokenCredential _credential;

    public RegistrationParsingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        // Support cross-tenant resource access by trying multiple authentication strategies:
        // 1. First, try default credentials in the current/home tenant (works for same-tenant resources)
        // 2. If configured, also try the specific resource tenant (for cross-tenant scenarios)
        var credentialChain = new List<TokenCredential>();
        
        // Always try default credentials first (home tenant or explicitly logged-in tenant)
        var defaultOptions = new DefaultAzureCredentialOptions
        {
            ExcludeAzurePowerShellCredential = true
        };
        credentialChain.Add(new DefaultAzureCredential(defaultOptions));
        
        // If a resource tenant is configured, also try credentials in that tenant
        var resourceTenantId = _configuration["AzureAI:TenantId"];
        if (!string.IsNullOrWhiteSpace(resourceTenantId))
        {
            var resourceTenantOptions = new DefaultAzureCredentialOptions
            {
                ExcludeAzurePowerShellCredential = true,
                TenantId = resourceTenantId
            };
            credentialChain.Add(new DefaultAzureCredential(resourceTenantOptions));
        }
        
        // Use ChainedTokenCredential to try each credential in order
        _credential = new ChainedTokenCredential(credentialChain.ToArray());
    }

    /// <summary>
    /// Parses registration CSV text using Azure AI Foundry API.
    /// Returns a list of ParsedRegistrant objects with status and optional error reasons for failed rows.
    /// Supports partial success: successfully parsed rows are returned alongside failed rows.
    /// </summary>
    /// <param name="csvText">CSV content as text (including headers and data rows).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of parsed registrants with success/failure status.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Azure AI configuration is missing or API call fails.</exception>
    public async Task<List<ParsedRegistrant>> ParseRegistrationsAsync(
        string csvText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(csvText))
            return new List<ParsedRegistrant>();

        var endpoint = _configuration["AzureAI:Endpoint"];
        var projectName = _configuration["AzureAI:ProjectName"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(projectName))
        {
            throw new InvalidOperationException(
                "Azure AI Foundry configuration is missing. Ensure AzureAI:Endpoint and AzureAI:ProjectName are configured.");
        }

        try
        {
            // Get access token using DefaultAzureCredential (Entra ID)
            // Use the Azure AI Foundry scope for Projects API authentication
            var tokenRequestContext = new Azure.Core.TokenRequestContext(
                scopes: new[] { "https://ai.azure.com/.default" });
            var tokenResult = await _credential.GetTokenAsync(tokenRequestContext, ct);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenResult.Token}");

            // Build the AI Foundry REST API endpoint
            // The endpoint can be either:
            // 1. Azure OpenAI format: https://{resource}.openai.azure.com
            // 2. Azure AI Services format: https://{resource}.services.ai.azure.com/api/projects/{project}
            // This code expects the endpoint to be the base URL without the /chat/completions path
            var aiEndpoint = $"{endpoint?.TrimEnd('/')}/openai/deployments/{projectName}/chat/completions?api-version=2024-10-21";

            // Structured prompt to extract registrant data from CSV
            var systemPrompt = @"You are a CSV data extraction assistant. Extract registrant information from provided CSV text.
For each row in the CSV:
1. Extract email (required) - must be valid email format
2. Extract first name (required) - apply title case (e.g., 'Kevin', not 'kevin' or 'KEVIN')
3. Extract last name (required) - apply title case
4. Extract registration datetime (optional) - if present, normalize to ISO 8601 UTC format; if absent, omit

If any required field is missing or invalid:
- Set status to 'failed'
- Provide a clear error reason (e.g., 'missing email', 'invalid email format', 'missing first name')

Return ONLY a valid JSON array with no markdown, code blocks, or explanation. Each object must have:
- email: string (empty if failed)
- firstName: string (empty if failed)
- lastName: string (empty if failed)
- registeredAt: ISO 8601 datetime string or null
- status: 'success' or 'failed'
- errorReason: string (only if status is 'failed')

Example output:
[{""email"":""john.doe@example.com"",""firstName"":""John"",""lastName"":""Doe"",""registeredAt"":""2024-01-15T10:30:00Z"",""status"":""success"",""errorReason"":null}]";

            var userMessage = $"Extract registrant information from this CSV:\n\n{csvText}";

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.3,
                top_p = 0.9,
                max_tokens = 4096,
                model = projectName  // Use project name as the model deployment
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(aiEndpoint, jsonContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Azure AI Foundry API call failed with status {response.StatusCode}: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var jsonResponse = JsonDocument.Parse(content);

            // Extract the assistant's response from the API response
            var assistantMessage = jsonResponse
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(assistantMessage))
            {
                throw new InvalidOperationException("Empty response from Azure AI Foundry API.");
            }

            // Parse the JSON array from the assistant's response
            // Handle cases where the response may contain markdown code blocks
            var jsonArrayString = assistantMessage.Trim();
            if (jsonArrayString.StartsWith("```"))
            {
                // Remove markdown code block markers if present
                var lines = jsonArrayString.Split('\n');
                jsonArrayString = string.Join('\n', lines.Skip(1).SkipLast(1)).Trim();
            }

            var registrants = JsonSerializer.Deserialize<List<ParsedRegistrant>>(jsonArrayString)
                ?? new List<ParsedRegistrant>();

            return registrants;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Failed to communicate with Azure AI Foundry API: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse Azure AI Foundry response as JSON: {ex.Message}", ex);
        }
    }
}
