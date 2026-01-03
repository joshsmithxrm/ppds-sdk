using FluentAssertions;
using PPDS.Auth.Profiles;
using Xunit;

namespace PPDS.Auth.Tests.Profiles;

public class ProfileEncryptionTests
{
    [Fact]
    public void Encrypt_ValidValue_ReturnsEncryptedWithPrefix()
    {
        var value = "my-secret-value";

        var encrypted = ProfileEncryption.Encrypt(value);

        encrypted.Should().StartWith("ENCRYPTED:");
        encrypted.Should().NotContain(value);
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrip()
    {
        var original = "my-secret-value";

        var encrypted = ProfileEncryption.Encrypt(original);
        var decrypted = ProfileEncryption.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Encrypt_NullOrEmpty_ReturnsEmpty(string? value)
    {
        var encrypted = ProfileEncryption.Encrypt(value);

        encrypted.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Decrypt_NullOrEmpty_ReturnsEmpty(string? value)
    {
        var decrypted = ProfileEncryption.Decrypt(value);

        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_WithoutPrefix_StillWorks()
    {
        var original = "test-value";
        var encrypted = ProfileEncryption.Encrypt(original);
        var withoutPrefix = encrypted.Replace("ENCRYPTED:", "");

        var decrypted = ProfileEncryption.Decrypt(withoutPrefix);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Decrypt_InvalidBase64_ReturnsAsIs()
    {
        var invalid = "not-valid-base64!@#$%";

        var result = ProfileEncryption.Decrypt(invalid);

        result.Should().Be(invalid);
    }

    [Fact]
    public void IsEncrypted_WithPrefix_ReturnsTrue()
    {
        var encrypted = ProfileEncryption.Encrypt("test");

        var result = ProfileEncryption.IsEncrypted(encrypted);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsEncrypted_WithoutPrefix_ReturnsFalse()
    {
        var plaintext = "plain-value";

        var result = ProfileEncryption.IsEncrypted(plaintext);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsEncrypted_NullOrEmpty_ReturnsFalse(string? value)
    {
        var result = ProfileEncryption.IsEncrypted(value);

        result.Should().BeFalse();
    }

    [Fact]
    public void Encrypt_DifferentValues_ProduceDifferentCiphertext()
    {
        var value1 = "first-secret";
        var value2 = "second-secret";

        var encrypted1 = ProfileEncryption.Encrypt(value1);
        var encrypted2 = ProfileEncryption.Encrypt(value2);

        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Encrypt_SameValue_BothDecryptCorrectly()
    {
        // DPAPI on Windows is non-deterministic (includes randomness), so same value produces different ciphertext
        // But both should decrypt to the same original value
        var value = "test-secret";

        var encrypted1 = ProfileEncryption.Encrypt(value);
        var encrypted2 = ProfileEncryption.Encrypt(value);

        ProfileEncryption.Decrypt(encrypted1).Should().Be(value);
        ProfileEncryption.Decrypt(encrypted2).Should().Be(value);
    }

    [Fact]
    public void Encrypt_SpecialCharacters_RoundTrips()
    {
        var value = "secret!@#$%^&*(){}[]|\\:;\"'<>,.?/~`";

        var encrypted = ProfileEncryption.Encrypt(value);
        var decrypted = ProfileEncryption.Decrypt(encrypted);

        decrypted.Should().Be(value);
    }

    [Fact]
    public void Encrypt_Unicode_RoundTrips()
    {
        var value = "秘密🔒パスワード";

        var encrypted = ProfileEncryption.Encrypt(value);
        var decrypted = ProfileEncryption.Decrypt(encrypted);

        decrypted.Should().Be(value);
    }

    [Fact]
    public void Encrypt_LongValue_RoundTrips()
    {
        var value = new string('x', 10000);

        var encrypted = ProfileEncryption.Encrypt(value);
        var decrypted = ProfileEncryption.Decrypt(encrypted);

        decrypted.Should().Be(value);
    }
}
