using System.Runtime.CompilerServices;

namespace Tests;

/// <summary>
/// Module initializer that runs BEFORE any type in this assembly is loaded.
/// </summary>
internal static class ModuleInit
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Keycloak
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_USERNAME", "test-admin");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD", "test-password");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_URL", "http://localhost:8080");
        Environment.SetEnvironmentVariable("KEYCLOAK_REALM", "test-realm");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_CLIENT_ID", "admin-cli");
        
        // Database
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_NAME", "testdb");
        Environment.SetEnvironmentVariable("DB_USER", "testuser");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");
        
        // RabbitMQ
        Environment.SetEnvironmentVariable("RABBITMQ_HOST", "localhost");
        Environment.SetEnvironmentVariable("RABBITMQ_USER", "testuser");
        Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", "testpass");
    }
}
