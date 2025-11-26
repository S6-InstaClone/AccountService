namespace AccountService.Messages
{
    /// <summary>
    /// Event published when a user account is deleted (GDPR compliance)
    /// All services should listen for this and delete associated user data
    /// </summary>
    public record AccountDeletedEvent
    {
        /// <summary>
        /// Keycloak user ID (UUID format string)
        /// </summary>
        public string UserId { get; init; } = string.Empty;

        public string? Username { get; init; }

        public string? Email { get; init; }

        public DateTime DeletedAt { get; init; }

        /// <summary>
        /// Reason for deletion (e.g., "GDPR_USER_REQUEST", "ADMIN_ACTION")
        /// </summary>
        public string Reason { get; init; } = "GDPR_USER_REQUEST";
    }
}