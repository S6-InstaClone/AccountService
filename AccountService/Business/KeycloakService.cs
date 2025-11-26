using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountService.Business
{
    public class KeycloakService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<KeycloakService> _logger;

        public KeycloakService(HttpClient httpClient, IConfiguration config, ILogger<KeycloakService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> DeleteUserAsync(string keycloakUserId)
        {
            try
            {
                var token = await GetAdminTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Failed to get Keycloak admin token");
                    return false;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var keycloakUrl = _config["Keycloak:AdminUrl"] ?? "http://keycloak:8080";
                var realm = _config["Keycloak:Realm"] ?? "instaclone";

                var deleteUrl = $"{keycloakUrl}/admin/realms/{realm}/users/{keycloakUserId}";
                _logger.LogDebug("Deleting user from Keycloak: {Url}", deleteUrl);

                var response = await _httpClient.DeleteAsync(deleteUrl);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation("Successfully deleted user {UserId} from Keycloak", keycloakUserId);
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("User {UserId} not found in Keycloak (may have been already deleted)", keycloakUserId);
                    return true; // Consider this a success
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete user {UserId} from Keycloak: {Status} - {Error}",
                        keycloakUserId, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while deleting user {UserId} from Keycloak", keycloakUserId);
                return false;
            }
        }

        private async Task<string?> GetAdminTokenAsync()
        {
            try
            {
                var keycloakUrl = _config["Keycloak:AdminUrl"] ?? "http://keycloak:8080";
                var clientId = _config["Keycloak:AdminClientId"] ?? "admin-cli";
                var adminUsername = _config["Keycloak:AdminUsername"] ?? "admin";
                var adminPassword = _config["Keycloak:AdminPassword"] ?? "admin";

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("username", adminUsername),
                    new KeyValuePair<string, string>("password", adminPassword),
                    new KeyValuePair<string, string>("grant_type", "password")
                });

                var tokenUrl = $"{keycloakUrl}/realms/master/protocol/openid-connect/token";
                _logger.LogDebug("Getting Keycloak admin token from: {Url}", tokenUrl);

                var response = await _httpClient.PostAsync(tokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get Keycloak admin token: {Status} - {Error}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                return result?.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while getting Keycloak admin token");
                return null;
            }
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}