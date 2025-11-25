namespace AccountService.Business
{
    public class KeycloakService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public KeycloakService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<bool> DeleteUserAsync(string keycloakUserId)
        {
            var token = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync(
                $"http://keycloak:18080/admin/realms/myrealm/users/{keycloakUserId}");

            return response.IsSuccessStatusCode;
        }

        private async Task<string> GetAdminTokenAsync()
        {
            var content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", "admin"),
            new KeyValuePair<string, string>("grant_type", "password")
        });

            var response = await _httpClient.PostAsync(
                "http://keycloak:18080/realms/master/protocol/openid-connect/token", content);

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            return result.AccessToken;
        }

        private record TokenResponse(string AccessToken);
    }
}
