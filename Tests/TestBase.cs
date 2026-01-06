namespace Tests;

/// <summary>
/// Base class for all tests. Static constructor ensures env vars are set.
/// NOTE: For tests that create KeycloakService instances directly, you MUST also
/// add a static constructor to that test class (static initialization order isn't guaranteed).
/// </summary>
public abstract class TestBase
{
    // Static constructor runs when the type is first accessed
    static TestBase()
    {
        SetRequiredEnvVars();
    }

    protected TestBase()
    {
        // Double-check env vars are set (belt and suspenders)
        SetRequiredEnvVars();
    }

    /// <summary>
    /// Sets all required environment variables for testing.
    /// Safe to call multiple times.
    /// </summary>
    public static void SetRequiredEnvVars()
    {
        // Keycloak env vars
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_USERNAME", "test-admin");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD", "test-password");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_URL", "http://localhost:8080");
        Environment.SetEnvironmentVariable("KEYCLOAK_REALM", "test-realm");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_CLIENT_ID", "admin-cli");
        
        // Azure Blob Storage env vars
        Environment.SetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net");
        Environment.SetEnvironmentVariable("AZURE_STORAGE_CONTAINER_NAME", "test-container");
        
        // Database env vars
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_NAME", "testdb");
        Environment.SetEnvironmentVariable("DB_USER", "testuser");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");
    }
}
