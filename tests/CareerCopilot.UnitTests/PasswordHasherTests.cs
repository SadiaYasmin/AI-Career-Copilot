using CareerCopilot.Infrastructure.Authentication;
using FluentAssertions;

namespace CareerCopilot.UnitTests;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashAndVerify_RoundTrips()
    {
        var hash = _hasher.Hash("P@ssw0rd!");

        _hasher.Verify("P@ssw0rd!", hash).Should().BeTrue();
    }

    [Fact]
    public void WrongPassword_FailsVerification()
    {
        var hash = _hasher.Hash("P@ssw0rd!");

        _hasher.Verify("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void SamePassword_ProducesDifferentHashes()
    {
        var one = _hasher.Hash("P@ssw0rd!");
        var two = _hasher.Hash("P@ssw0rd!");

        one.Should().NotBe(two);
    }

    [Fact]
    public void MalformedHash_FailsVerification()
    {
        _hasher.Verify("anything", "not-a-valid-hash").Should().BeFalse();
        _hasher.Verify("anything", string.Empty).Should().BeFalse();
    }
}