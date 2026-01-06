using System.Runtime.CompilerServices;

namespace Tests;

/// <summary>
/// Module initializer to set required environment variables before any tests run.
/// This ensures KeycloakService can be instantiated during mocking.
/// </summary>
public static class TestInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Set dummy values for Keycloak env vars so KeycloakService constructor doesn't throw
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_USERNAME", "test-admin");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD", "test-password");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_URL", "http://localhost:8080");
        Environment.SetEnvironmentVariable("KEYCLOAK_REALM", "test-realm");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_CLIENT_ID", "admin-cli");
    }
}
