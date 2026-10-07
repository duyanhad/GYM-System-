using System;
using System.Linq;
using GYM_Management_System.Services;

namespace GYM_Management_System.UnitTests;

public class CodeGeneratorTests
{
    [Fact]
    public void MemberCode_ShouldBePaddedToFiveDigits()
    {
        Assert.Equal("MEM00001", CodeGenerator.MemberCode(1));
        Assert.Equal("MEM00123", CodeGenerator.MemberCode(123));
    }

    [Fact]
    public void SubscriptionCode_ShouldIncludeDateAndSequence()
    {
        var code = CodeGenerator.SubscriptionCode(new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), 7);

        Assert.Equal("SUB20261008-0007", code);
    }

    [Fact]
    public void InvoiceAndPaymentCode_ShouldFollowPrefix()
    {
        var date = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        Assert.StartsWith("INV20260102-", CodeGenerator.InvoiceCode(date, 1));
        Assert.StartsWith("PAY20260102-", CodeGenerator.PaymentCode(date, 1));
    }

    [Fact]
    public void OtpCode_ShouldAlwaysBeSixDigits()
    {
        for (var i = 0; i < 25; i++)
        {
            var otp = CodeGenerator.OtpCode();

            Assert.Equal(6, otp.Length);
            Assert.True(otp.All(char.IsDigit));
        }
    }

    [Fact]
    public void SecureToken_ShouldNotContainUrlUnsafeCharacters()
    {
        var token = CodeGenerator.SecureToken();

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }
}
