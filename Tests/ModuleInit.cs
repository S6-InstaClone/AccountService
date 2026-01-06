using System.Runtime.CompilerServices;

namespace Tests;

/// <summary>
/// Module initializer that runs BEFORE any type in this assembly is loaded.
/// This is the earliest possible point to set environment variables.
/// </summary>
internal static class ModuleInit
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Set all required environment variables FIRST
        // This runs before any test class static constructors
        
        // Keycloak
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_USERNAME", "test-admin");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD", "test-password");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_URL", "http://localhost:8080");
        Environment.SetEnvironmentVariable("KEYCLOAK_REALM", "test-realm");
        Environment.SetEnvironmentVariable("KEYCLOAK_ADMIN_CLIENT_ID", "admin-cli");
        
        // Azure Blob Storage
        Environment.SetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", 
            "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net");
        Environment.SetEnvironmentVariable("AZURE_STORAGE_CONTAINER_NAME", "test-container");
        
        // Database
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_NAME", "testdb");
        Environment.SetEnvironmentVariable("DB_USER", "testuser");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");
    }
}
