using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

// ---------------------------------------------------------------------------
// Models
// ---------------------------------------------------------------------------

public class PaymentDto
{
    public string? Method { get; set; }
    public string? CardNumber { get; set; }
    public string? Cvv { get; set; }
    public string? CardHolder { get; set; }
    public string? Iban { get; set; }
    public string? BankName { get; set; }
    public string? PaypalEmail { get; set; }
    public decimal Amount { get; set; }
}

public class DocumentDto
{
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
}

// ---------------------------------------------------------------------------
// Validators
// ---------------------------------------------------------------------------

public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        RuleFor(x => x.Amount).Positive();

        RuleSwitch(x => x.Method)
            .Case("credit_card", rules =>
            {
                rules.RuleFor(x => x.CardNumber).NotEmpty();
                rules.RuleFor(x => x.Cvv).NotEmpty().MinimumLength(3).MaximumLength(4);
                rules.RuleFor(x => x.CardHolder).NotEmpty();
            })
            .Case("bank_transfer", rules =>
            {
                rules.RuleFor(x => x.Iban).NotEmpty();
                rules.RuleFor(x => x.BankName).NotEmpty();
            })
            .Case("paypal", rules =>
            {
                rules.RuleFor(x => x.PaypalEmail).NotEmpty().Email();
            })
            .Default(rules =>
            {
                rules.RuleFor(x => x.Amount).GreaterThan(0m);
            });
    }
}

public class PaymentValidatorWithAsync : AbstractValidator<PaymentDto>
{
    public PaymentValidatorWithAsync()
    {
        RuleSwitch(x => x.Method)
            .Case("credit_card", rules =>
            {
                rules.RuleFor(x => x.CardNumber)
                    .NotEmpty()
                    .MustAsync(async cardNumber =>
                    {
                        await Task.Delay(1);
                        return cardNumber != null && cardNumber.Length == 16;
                    });
            });
    }
}

public class DocumentValidator : AbstractValidator<DocumentDto>
{
    public DocumentValidator()
    {
        RuleFor(x => x.DocumentNumber)
            .SwitchOn(x => x.DocumentType)
            .Case("passport", b => b.NotEmpty().Matches(@"^[A-Z]{2}\d{6}$"))
            .Case("dni",      b => b.NotEmpty().IsNumeric().MinimumLength(8).MaximumLength(8))
            .Case("ruc",      b => b.NotEmpty().IsNumeric().MinimumLength(11).MaximumLength(11))
            .Default(         b => b.NotEmpty());
    }
}

// ---------------------------------------------------------------------------
// RuleSwitch tests
// ---------------------------------------------------------------------------

public class RuleSwitchTests
{
    private readonly PaymentValidator _validator = new PaymentValidator();

    [Fact]
    public void RuleSwitch_CreditCard_WhenCardNumberEmpty_Fails()
    {
        var dto = new PaymentDto
        {
            Method = "credit_card",
            CardNumber = "",
            Cvv = "123",
            CardHolder = "John Doe",
            Amount = 100m
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("CardNumber"));
    }

    [Fact]
    public void RuleSwitch_CreditCard_WhenValid_Passes()
    {
        var dto = new PaymentDto
        {
            Method = "credit_card",
            CardNumber = "4111111111111111",
            Cvv = "123",
            CardHolder = "John Doe",
            Amount = 100m
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RuleSwitch_BankTransfer_ValidatesIban()
    {
        var dto = new PaymentDto
        {
            Method = "bank_transfer",
            Iban = "DE89370400440532013000",
            BankName = "Deutsche Bank",
            Amount = 100m
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RuleSwitch_BankTransfer_WhenIbanEmpty_Fails()
    {
        var dto = new PaymentDto
        {
            Method = "bank_transfer",
            Iban = "",
            BankName = "Deutsche Bank",
            Amount = 100m
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Iban"));
    }

    [Fact]
    public void RuleSwitch_Default_WhenNoMatchingCase_FailsWhenAmountIsZero()
    {
        var dto = new PaymentDto
        {
            Method = "unknown",
            Amount = 0m
        };

        var result = _validator.Validate(dto);

        // Amount rule from RuleFor fires (Positive), and default case GreaterThan(0) also fires
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RuleSwitch_Default_WhenNoMatchingCase_PassesWhenAmountPositive()
    {
        var dto = new PaymentDto
        {
            Method = "unknown",
            Amount = 50m
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RuleSwitch_CreditCard_DoesNotValidateBankFields()
    {
        var dto = new PaymentDto
        {
            Method = "credit_card",
            CardNumber = "4111111111111111",
            Cvv = "123",
            CardHolder = "John Doe",
            Iban = "",        // empty but should not matter
            BankName = "",    // empty but should not matter
            Amount = 100m
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.False(result.HasErrorFor("Iban"));
        Assert.False(result.HasErrorFor("BankName"));
    }

    [Fact]
    public void RuleSwitch_BankTransfer_DoesNotValidateCardFields()
    {
        var dto = new PaymentDto
        {
            Method = "bank_transfer",
            CardNumber = "",    // empty but should not matter
            Cvv = "",           // empty but should not matter
            CardHolder = "",    // empty but should not matter
            Iban = "DE89370400440532013000",
            BankName = "Deutsche Bank",
            Amount = 100m
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.False(result.HasErrorFor("CardNumber"));
        Assert.False(result.HasErrorFor("Cvv"));
        Assert.False(result.HasErrorFor("CardHolder"));
    }

    [Fact]
    public async Task RuleSwitch_AsyncRule_InCase_Works()
    {
        var asyncValidator = new PaymentValidatorWithAsync();

        var validDto = new PaymentDto
        {
            Method = "credit_card",
            CardNumber = "1234567890123456"   // exactly 16 digits
        };

        var invalidDto = new PaymentDto
        {
            Method = "credit_card",
            CardNumber = "123"   // too short
        };

        var validResult = await asyncValidator.ValidateAsync(validDto);
        var invalidResult = await asyncValidator.ValidateAsync(invalidDto);

        Assert.True(validResult.IsValid);
        Assert.False(invalidResult.IsValid);
        Assert.True(invalidResult.HasErrorFor("CardNumber"));
    }
}

// ---------------------------------------------------------------------------
// SwitchOn tests
// ---------------------------------------------------------------------------

public class SwitchOnTests
{
    private readonly DocumentValidator _validator = new DocumentValidator();

    [Fact]
    public void SwitchOn_Passport_WhenPatternMatches_Passes()
    {
        var dto = new DocumentDto
        {
            DocumentType = "passport",
            DocumentNumber = "AB123456"
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SwitchOn_Passport_WhenPatternFails_ReturnsError()
    {
        var dto = new DocumentDto
        {
            DocumentType = "passport",
            DocumentNumber = "123"   // doesn't match ^[A-Z]{2}\d{6}$
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("DocumentNumber"));
    }

    [Fact]
    public void SwitchOn_Dni_WhenCorrectLength_Passes()
    {
        var dto = new DocumentDto
        {
            DocumentType = "dni",
            DocumentNumber = "12345678"
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SwitchOn_Dni_WhenWrongLength_Fails()
    {
        var dto = new DocumentDto
        {
            DocumentType = "dni",
            DocumentNumber = "123"   // too short
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("DocumentNumber"));
    }

    [Fact]
    public void SwitchOn_Default_WhenNoMatchingCase_AppliesDefault()
    {
        // Non-empty → passes
        var passingDto = new DocumentDto
        {
            DocumentType = "other",
            DocumentNumber = "ANYTHING"
        };

        // Empty → fails
        var failingDto = new DocumentDto
        {
            DocumentType = "other",
            DocumentNumber = ""
        };

        var passingResult = _validator.Validate(passingDto);
        var failingResult = _validator.Validate(failingDto);

        Assert.True(passingResult.IsValid);
        Assert.False(failingResult.IsValid);
        Assert.True(failingResult.HasErrorFor("DocumentNumber"));
    }

    [Fact]
    public void SwitchOn_WhenCaseDoesNotMatch_OtherCaseRulesNotApplied()
    {
        // DocumentType="dni" → passport regex should NOT be checked
        var dto = new DocumentDto
        {
            DocumentType = "dni",
            DocumentNumber = "12345678"   // valid DNI, but would fail passport regex
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }
}
