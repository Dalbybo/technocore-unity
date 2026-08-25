using System;
using System.Numerics;
using System.Text;

namespace Technocore
{
    /// <summary>
    /// Anything that can sign for a <c>did:key</c>. Unity has no built-in Ed25519,
    /// so signing is pluggable: implement this with your Ed25519 library of choice
    /// (BouncyCastle, Chaos.NaCl, the Technocore.NET package, …).
    /// </summary>
    public interface ITechnocoreSigner
    {
        /// <summary>The signer's <c>did:key:z6Mk…</c> identifier.</summary>
        string Did { get; }

        /// <summary>Sign the canonical <c>room|nonce|text</c> payload, returning base64url.</summary>
        string Sign(string room, string nonce, string text);
    }

    /// <summary>Helpers for encoding/decoding Ed25519 <c>did:key</c> identifiers.</summary>
    public static class Did
    {
        private static readonly byte[] MulticodecEd25519 = { 0xed, 0x01 };

        /// <summary>Encode a raw 32-byte Ed25519 public key as a <c>did:key</c>.</summary>
        public static string Encode(byte[] publicKey)
        {
            var body = new byte[2 + publicKey.Length];
            Buffer.BlockCopy(MulticodecEd25519, 0, body, 0, 2);
            Buffer.BlockCopy(publicKey, 0, body, 2, publicKey.Length);
            return "did:key:z" + Base58.Encode(body);
        }

        /// <summary>Recover the raw 32-byte Ed25519 public key from a <c>did:key</c>.</summary>
        public static byte[] Decode(string did)
        {
            if (!did.StartsWith("did:key:z", StringComparison.Ordinal))
                throw new FormatException("not a did:key identifier");
            var decoded = Base58.Decode(did.Substring("did:key:z".Length));
            if (decoded.Length < 2 || decoded[0] != 0xed || decoded[1] != 0x01)
                throw new FormatException("did:key is not an Ed25519 key");
            var key = new byte[decoded.Length - 2];
            Buffer.BlockCopy(decoded, 2, key, 0, key.Length);
            return key;
        }

        /// <summary>A strictly increasing nanosecond nonce.</summary>
        public static string FreshNonce() =>
            (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L
             + (DateTime.UtcNow.Ticks % 1_000_000L)).ToString();
    }

    /// <summary>An anonymous, unsigned poster (a plain nickname). Good for NPC/spectator chatter.</summary>
    public sealed class NickIdentity : ITechnocoreSigner
    {
        private readonly string _nick;
        public NickIdentity(string nick) => _nick = nick;
        public string Did => _nick;               // used as the "from" nick for unsigned posts
        public string Sign(string room, string nonce, string text) => null; // unsigned
    }
}
