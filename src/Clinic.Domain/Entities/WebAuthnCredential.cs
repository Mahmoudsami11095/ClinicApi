using System;

namespace Clinic.Domain.Entities;

public class WebAuthnCredential
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    
    // FIDO2 standard fields (stored as Base64 strings for simplicity in this architecture)
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public uint SignCount { get; set; }
    public string AuthenticatorAttachment { get; set; } = string.Empty; // cross-platform, platform
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
