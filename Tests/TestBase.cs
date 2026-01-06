namespace Tests;

/// <summary>
/// Base class for all tests. Sets required environment variables.
/// </summary>
public abstract class TestBase
{
    static TestBase()
    {
        SetRequiredEnvVars();
    }

    protected TestBase()
    {
        SetRequiredEnvVars();
    }

    public static void SetRequiredEnvVars()
    {
        // Keycloak env vars
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_USERNAME", "test-admin");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD", "test-password");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_URL", "http://localhost:8080");
        Environment.SetEnvironmentVariable("KEYCLOAK_REALM", "test-realm");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_CLIENT_ID", "admin-cli");
        
        // Database env vars
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_NAME", "testdb");
        Environment.SetEnvironmentVariable("DB_USER", "testuser");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");
        
        // RabbitMQ env vars
        Environment.SetEnvironmentVariable("RABBITMQ_HOST", "localhost");
        Environment.SetEnvironmentVariable("RABBITMQ_USER", "testuser");
        Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", "testpass");
    }
}
