# Switch / Case — Advanced Conditional Validation

This document describes two complementary features for conditional validation in Vali-Validation: **`RuleSwitch`** and **`SwitchOn`**. Both allow applying different sets of rules depending on a runtime value, but they operate at different levels of granularity and serve distinct purposes.

---

## Overview

Real-world models rarely have a flat validation schema. A payment request behaves differently depending on whether the method is a credit card, bank transfer, or PayPal. A document number has different format requirements depending on whether the document is a passport or a national identity card. A loan application has different mandatory fields depending on the loan type.

Vali-Validation provides two dedicated APIs to handle this:

| Feature | Level | Key question |
|---|---|---|
| `RuleSwitch` | Validator (multiple properties) | "Which group of rules should I run for the whole object?" |
| `SwitchOn` | Property (single property) | "Which rules apply to *this specific field* depending on another field?" |

Both features share the same core semantics:

- **Exclusivity**: only one case executes per validation run. As soon as a matching key is found, the remaining cases are skipped.
- **Default fallback**: an optional `.Default(...)` branch runs when no case matches.
- **No match, no Default**: if no case matches and no Default is defined, the switch block produces no errors (it is silent, not an error in itself).
- **Global rules are unaffected**: rules defined outside the switch always execute.

---

## Comparison at a Glance

| | `RuleSwitch` | `SwitchOn` |
|---|---|---|
| Where it is called | Inside `AbstractValidator<T>` constructor | Chained on a `RuleFor(...)` call |
| Discriminator | Any property of the root object | Any property of the root object |
| Scope of each case | Multiple properties, full rule sets | A single property |
| Interface returned | `ICaseBuilder<T, TKey>` | `ISwitchOnBuilder<T, TProperty, TKey>` |
| Case body type | `Action<AbstractValidator<T>>` | `Action<IRuleBuilder<T, TProperty>>` |
| Global rules mixed in | Yes (defined outside the switch) | Yes (defined before `.SwitchOn(...)`) |
| Async rule support | Yes (`MustAsync`, `DependentRuleAsync`) | Yes (`MustAsync`, `WhenAsync`) |

---

## Part 1: RuleSwitch

### What Is RuleSwitch

`RuleSwitch` is a validator-level switch/case block. It is defined inside the constructor of a class that inherits `AbstractValidator<T>`. It reads a discriminator value from the object being validated and applies the matching case's rules — rules that can span multiple properties.

The method signature is:

```csharp
protected ICaseBuilder<T, TKey> RuleSwitch<TKey>(Expression<Func<T, TKey>> keyExpression)
```

### ICaseBuilder Interface

```csharp
public interface ICaseBuilder<T, TKey> where T : class
{
    // Applies rules when the discriminator equals value
    ICaseBuilder<T, TKey> Case(TKey value, Action<AbstractValidator<T>> configure);

    // Applies rules when no case matches
    ICaseBuilder<T, TKey> Default(Action<AbstractValidator<T>> configure);
}
```

The `configure` delegate receives an `AbstractValidator<T>` instance, so you call `RuleFor(...)` inside it exactly as you would in a normal validator constructor.

### How It Works Internally

When you call `.Case(...)` or `.Default(...)` for the first time, the builder registers exactly two delegates on the parent validator (one synchronous, one asynchronous). At validation time, the discriminator is evaluated, the matching case's rules are executed, and the results are merged into the main `ValidationResult`. No case is ever executed more than once per validation run.

---

### Example 1 — Payment Method (String Discriminator)

#### Models

```csharp
public class PaymentDto
{
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;  // "credit_card" | "bank_transfer" | "paypal"

    // Credit card fields
    public string? CardNumber { get; set; }
    public string? Cvv { get; set; }
    public DateTime? ExpirationDate { get; set; }

    // Bank transfer fields
    public string? Iban { get; set; }
    public string? BankCode { get; set; }

    // PayPal fields
    public string? PaypalEmail { get; set; }

    // Fallback
    public string? Reference { get; set; }
}
```

#### Validator

```csharp
public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        // Global rules — always execute regardless of Method
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("The Amount field must be greater than zero.");

        RuleFor(x => x.Method)
            .NotEmpty()
            .WithMessage("The Method field is required.");

        // Switch: exactly one case executes based on x.Method
        RuleSwitch(x => x.Method)
            .Case("credit_card", rules =>
            {
                rules.RuleFor(x => x.CardNumber)
                    .NotEmpty()
                        .WithMessage("The CardNumber field is required for credit card payments.")
                    .CreditCard()
                        .WithMessage("The CardNumber field must be a valid credit card number.");

                rules.RuleFor(x => x.Cvv)
                    .NotEmpty()
                        .WithMessage("The Cvv field is required.")
                    .MinimumLength(3)
                    .MaximumLength(4)
                        .WithMessage("The Cvv field must be between 3 and 4 characters.");

                rules.RuleFor(x => x.ExpirationDate)
                    .NotNull()
                        .WithMessage("The ExpirationDate field is required.")
                    .FutureDate()
                        .WithMessage("The ExpirationDate must be in the future.");
            })
            .Case("bank_transfer", rules =>
            {
                rules.RuleFor(x => x.Iban)
                    .NotEmpty()
                        .WithMessage("The Iban field is required for bank transfer payments.")
                    .Iban()
                        .WithMessage("The Iban field must be a valid IBAN.");

                rules.RuleFor(x => x.BankCode)
                    .NotEmpty()
                        .WithMessage("The BankCode field is required.");
            })
            .Case("paypal", rules =>
            {
                rules.RuleFor(x => x.PaypalEmail)
                    .NotEmpty()
                        .WithMessage("The PaypalEmail field is required for PayPal payments.")
                    .Email()
                        .WithMessage("The PaypalEmail field must be a valid email address.");
            })
            .Default(rules =>
            {
                // Executed when Method is anything other than the three cases above
                rules.RuleFor(x => x.Reference)
                    .NotEmpty()
                        .WithMessage("The Reference field is required for this payment method.");
            });
    }
}
```

#### Usage

```csharp
var validator = new PaymentValidator();

// Scenario: credit card with missing CVV
var payment = new PaymentDto
{
    Amount = 150.00m,
    Method = "credit_card",
    CardNumber = "4111111111111111",
    Cvv = null,                        // missing
    ExpirationDate = DateTime.Now.AddYears(2)
};

var result = await validator.ValidateAsync(payment);
// result.IsValid == false
// result.Errors["Cvv"] == ["The Cvv field is required."]
// "Iban", "BankCode", "PaypalEmail" are NOT reported — those cases were skipped

// Scenario: bank transfer — passes
var transfer = new PaymentDto
{
    Amount = 500.00m,
    Method = "bank_transfer",
    Iban = "GB29NWBK60161331926819",
    BankCode = "NWBK"
};

var r2 = await validator.ValidateAsync(transfer);
// r2.IsValid == true
// Credit card fields are silently ignored
```

---

### Example 2 — User Type (Enum Discriminator)

Using an enum as the discriminator is a very common pattern. Enums give you compile-time safety and make the switch block easy to read.

#### Models and Enum

```csharp
public enum UserType
{
    Admin,
    Client,
    Guest
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserType Type { get; set; }

    // Admin-only
    public string? AdminAccessCode { get; set; }
    public IList<string> Permissions { get; set; } = new List<string>();

    // Client-only
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }

    // Guest-only
    public string? SessionToken { get; set; }
}
```

#### Validator

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        // Global — applies to every user type
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .IsAlphanumeric();

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email();

        RuleSwitch(x => x.Type)
            .Case(UserType.Admin, rules =>
            {
                rules.RuleFor(x => x.AdminAccessCode)
                    .NotEmpty()
                        .WithMessage("Admins must provide an access code.")
                    .MinimumLength(16)
                        .WithMessage("The admin access code must be at least 16 characters.");

                rules.RuleFor(x => x.Permissions)
                    .NotEmptyCollection()
                        .WithMessage("Admins must have at least one permission assigned.");
            })
            .Case(UserType.Client, rules =>
            {
                rules.RuleFor(x => x.CompanyName)
                    .NotEmpty()
                        .WithMessage("Company name is required for client accounts.");

                rules.RuleFor(x => x.TaxId)
                    .NotEmpty()
                        .WithMessage("Tax ID is required for client accounts.")
                    .Matches(@"^[A-Z0-9]{9,13}$")
                        .WithMessage("Tax ID must be between 9 and 13 uppercase alphanumeric characters.");
            })
            .Case(UserType.Guest, rules =>
            {
                rules.RuleFor(x => x.SessionToken)
                    .NotEmpty()
                        .WithMessage("A session token is required for guest access.")
                    .Guid()
                        .WithMessage("The session token must be a valid GUID.");
            });
        // No .Default() — if Type is an unknown enum value, only global rules run
    }
}
```

---

### Example 3 — Subscription Level (Integer Discriminator)

```csharp
public class SubscriptionDto
{
    public int Level { get; set; }           // 1=Free, 2=Pro, 3=Enterprise
    public string OrganizationName { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public string? BillingEmail { get; set; }
    public string? ContractNumber { get; set; }
    public string? SupportTier { get; set; }
}

public class SubscriptionValidator : AbstractValidator<SubscriptionDto>
{
    public SubscriptionValidator()
    {
        RuleFor(x => x.OrganizationName)
            .NotEmpty()
            .MaximumLength(200);

        RuleSwitch(x => x.Level)
            .Case(1, rules =>  // Free tier
            {
                // Free tier: max 5 users, no billing email required
                rules.RuleFor(x => x.MaxUsers)
                    .LessThanOrEqualTo(5)
                        .WithMessage("Free tier supports a maximum of 5 users.");
            })
            .Case(2, rules =>  // Pro tier
            {
                rules.RuleFor(x => x.MaxUsers)
                    .Between(1, 50)
                        .WithMessage("Pro tier supports between 1 and 50 users.");

                rules.RuleFor(x => x.BillingEmail)
                    .NotEmpty()
                        .WithMessage("A billing email is required for Pro subscriptions.")
                    .Email();
            })
            .Case(3, rules =>  // Enterprise tier
            {
                rules.RuleFor(x => x.MaxUsers)
                    .GreaterThan(0)
                        .WithMessage("Enterprise tier must have at least one user.");

                rules.RuleFor(x => x.BillingEmail)
                    .NotEmpty()
                    .Email();

                rules.RuleFor(x => x.ContractNumber)
                    .NotEmpty()
                        .WithMessage("A contract number is required for Enterprise subscriptions.")
                    .Matches(@"^ENT-\d{6}$")
                        .WithMessage("Contract number must match the format ENT-XXXXXX.");

                rules.RuleFor(x => x.SupportTier)
                    .NotEmpty()
                    .In(new[] { "standard", "premium", "dedicated" })
                        .WithMessage("Support tier must be one of: standard, premium, dedicated.");
            })
            .Default(rules =>
            {
                // Unknown level — reject it
                rules.RuleFor(x => x.Level)
                    .Must(_ => false)
                        .WithMessage("The Level field must be 1 (Free), 2 (Pro), or 3 (Enterprise).");
            });
    }
}
```

---

### Example 4 — Shipping Type (Complex Object, Multiple Addresses)

```csharp
public class ShipmentDto
{
    public string ShippingType { get; set; } = string.Empty; // "home_delivery" | "store_pickup" | "locker"
    public string RecipientName { get; set; } = string.Empty;

    // home_delivery fields
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }

    // store_pickup / locker fields
    public string? LocationCode { get; set; }

    // locker-specific
    public string? LockerPin { get; set; }
}

public class ShipmentValidator : AbstractValidator<ShipmentDto>
{
    public ShipmentValidator()
    {
        RuleFor(x => x.RecipientName)
            .NotEmpty()
            .MaximumLength(100);

        RuleSwitch(x => x.ShippingType)
            .Case("home_delivery", rules =>
            {
                rules.RuleFor(x => x.Street)
                    .NotEmpty()
                        .WithMessage("Street address is required for home delivery.");

                rules.RuleFor(x => x.City)
                    .NotEmpty()
                        .WithMessage("City is required for home delivery.");

                rules.RuleFor(x => x.PostalCode)
                    .NotEmpty()
                    .Matches(@"^\d{4,10}$")
                        .WithMessage("PostalCode must be 4 to 10 digits.");

                rules.RuleFor(x => x.CountryCode)
                    .NotEmpty()
                    .CountryCode()
                        .WithMessage("CountryCode must be a valid ISO 3166-1 alpha-2 code.");
            })
            .Case("store_pickup", rules =>
            {
                rules.RuleFor(x => x.LocationCode)
                    .NotEmpty()
                        .WithMessage("A store location code is required for store pickup.")
                    .Matches(@"^STR-\d{4}$")
                        .WithMessage("LocationCode must match the format STR-XXXX.");
            })
            .Case("locker", rules =>
            {
                rules.RuleFor(x => x.LocationCode)
                    .NotEmpty()
                        .WithMessage("A locker location code is required.")
                    .Matches(@"^LCK-\d{5}$")
                        .WithMessage("LocationCode must match the format LCK-XXXXX.");

                rules.RuleFor(x => x.LockerPin)
                    .NotEmpty()
                        .WithMessage("A PIN is required for locker delivery.")
                    .IsNumeric()
                    .MinimumLength(4)
                    .MaximumLength(6)
                        .WithMessage("The locker PIN must be 4 to 6 digits.");
            })
            .Default(rules =>
            {
                rules.RuleFor(x => x.ShippingType)
                    .Must(_ => false)
                        .WithMessage("ShippingType must be 'home_delivery', 'store_pickup', or 'locker'.");
            });
    }
}
```

---

### Example 5 — WithMessage and WithErrorCode Inside Cases

You can use `.WithMessage(...)` and `.WithErrorCode(...)` inside any case block exactly as you would in a regular rule chain. Error codes end up in `ValidationResult.ErrorCodes` alongside `ValidationResult.Errors`.

```csharp
public class LoanApplicationDto
{
    public string LoanType { get; set; } = string.Empty; // "personal" | "mortgage" | "vehicle"
    public decimal RequestedAmount { get; set; }
    public string? PropertyAddress { get; set; }
    public decimal? PropertyValue { get; set; }
    public string? VehicleVin { get; set; }
    public int? VehicleYear { get; set; }
}

public class LoanApplicationValidator : AbstractValidator<LoanApplicationDto>
{
    public LoanApplicationValidator()
    {
        RuleFor(x => x.RequestedAmount)
            .Positive()
            .WithMessage("The requested amount must be greater than zero.")
            .WithErrorCode("LOAN_001");

        RuleSwitch(x => x.LoanType)
            .Case("personal", rules =>
            {
                rules.RuleFor(x => x.RequestedAmount)
                    .LessThanOrEqualTo(50000m)
                    .WithMessage("Personal loans may not exceed $50,000.")
                    .WithErrorCode("LOAN_PERSONAL_001");
            })
            .Case("mortgage", rules =>
            {
                rules.RuleFor(x => x.PropertyAddress)
                    .NotEmpty()
                    .WithMessage("Property address is required for mortgage applications.")
                    .WithErrorCode("LOAN_MTG_001");

                rules.RuleFor(x => x.PropertyValue)
                    .NotNull()
                    .WithMessage("Property value is required for mortgage applications.")
                    .WithErrorCode("LOAN_MTG_002");
            })
            .Case("vehicle", rules =>
            {
                rules.RuleFor(x => x.VehicleVin)
                    .NotEmpty()
                    .WithMessage("Vehicle VIN is required for vehicle loans.")
                    .WithErrorCode("LOAN_VEH_001")
                    .Matches(@"^[A-HJ-NPR-Z0-9]{17}$")
                    .WithMessage("Vehicle VIN must be 17 valid characters.")
                    .WithErrorCode("LOAN_VEH_002");

                rules.RuleFor(x => x.VehicleYear)
                    .NotNull()
                    .WithMessage("Vehicle year is required.")
                    .WithErrorCode("LOAN_VEH_003");
            });
    }
}
```

---

### Example 6 — Nesting When/Unless Inside Cases

Rules inside a case block can themselves be conditional using `.When(...)` and `.Unless(...)`. This allows fine-grained sub-conditions without nesting another switch.

```csharp
public class InsurancePolicyDto
{
    public string PolicyType { get; set; } = string.Empty; // "life" | "health" | "property"
    public int Age { get; set; }
    public bool IsSmoker { get; set; }
    public decimal? CoverageAmount { get; set; }
    public string? PropertyAddress { get; set; }
    public bool HasExistingCondition { get; set; }
    public string? ConditionDetails { get; set; }
}

public class InsurancePolicyValidator : AbstractValidator<InsurancePolicyDto>
{
    public InsurancePolicyValidator()
    {
        RuleFor(x => x.Age)
            .Between(18, 99)
            .WithMessage("Applicant age must be between 18 and 99.");

        RuleSwitch(x => x.PolicyType)
            .Case("life", rules =>
            {
                rules.RuleFor(x => x.CoverageAmount)
                    .NotNull()
                    .WithMessage("Coverage amount is required for life insurance.");

                // Additional rule only for smokers: lower maximum coverage
                rules.RuleFor(x => x.CoverageAmount)
                    .LessThanOrEqualTo(500000m)
                    .WithMessage("Smokers are limited to $500,000 coverage on life policies.")
                    .When(x => x.IsSmoker);

                // Non-smokers can go up to 2 million
                rules.RuleFor(x => x.CoverageAmount)
                    .LessThanOrEqualTo(2000000m)
                    .WithMessage("Life insurance coverage may not exceed $2,000,000.")
                    .Unless(x => x.IsSmoker);
            })
            .Case("health", rules =>
            {
                // If the applicant has an existing condition, details are required
                rules.RuleFor(x => x.ConditionDetails)
                    .NotEmpty()
                    .WithMessage("Please describe your existing medical condition.")
                    .When(x => x.HasExistingCondition);
            })
            .Case("property", rules =>
            {
                rules.RuleFor(x => x.PropertyAddress)
                    .NotEmpty()
                    .WithMessage("Property address is required for property insurance.");
            });
    }
}
```

---

### Behavior Reference Table for RuleSwitch

| Scenario | Result |
|---|---|
| Discriminator matches a Case | Only that Case's rules execute |
| Discriminator matches no Case, Default defined | Default rules execute |
| Discriminator matches no Case, no Default | No errors from the switch block |
| Global rules + matching Case | Both global rules AND Case rules execute |
| Multiple Cases with the same key | Only the first one is used (first-match wins) |
| Async rules inside a Case | Execute during `ValidateAsync` only, not `Validate` |
| `Validate` (synchronous) called with async case rules | Sync rules in the case run; async rules are skipped |

---

## Part 2: SwitchOn

### What Is SwitchOn

`SwitchOn` is a property-level switch/case. It is chained after a `RuleFor(...)` call and applies different validation rules to the **same property** depending on the value of another property on the same object.

This is the right tool when you want to validate a single field differently based on context — a document number format, a measurement value in different units, a body payload in different formats.

The method signature on `IRuleBuilder<T, TProperty>` is:

```csharp
ISwitchOnBuilder<T, TProperty, TKey> SwitchOn<TKey>(Expression<Func<T, TKey>> keyExpression)
```

### ISwitchOnBuilder Interface

```csharp
public interface ISwitchOnBuilder<T, TProperty, TKey> where T : class
{
    // Applies rules to the property when the discriminator equals value
    ISwitchOnBuilder<T, TProperty, TKey> Case(TKey value, Action<IRuleBuilder<T, TProperty>> configure);

    // Applies rules to the property when no case matches
    ISwitchOnBuilder<T, TProperty, TKey> Default(Action<IRuleBuilder<T, TProperty>> configure);
}
```

The `configure` delegate receives an `IRuleBuilder<T, TProperty>` for the **same property** that was originally passed to `RuleFor`. Every rule method available on the builder (`NotEmpty`, `Matches`, `MinimumLength`, etc.) is available inside each case.

### How It Works Internally

When you call `SwitchOn(...)` on a `RuleBuilder<T, TProperty>`, it creates a `SwitchOnBuilder` that registers one sync and one async delegate on the parent validator. Each delegate evaluates the discriminator and runs only the matching case's rules against the original property value.

---

### Example 1 — Document Number by Document Type

```csharp
public class DocumentDto
{
    public string DocumentType { get; set; } = string.Empty; // "passport" | "national_id" | "tax_id" | "driver_license"
    public string DocumentNumber { get; set; } = string.Empty;
}

public class DocumentValidator : AbstractValidator<DocumentDto>
{
    public DocumentValidator()
    {
        RuleFor(x => x.DocumentType)
            .NotEmpty()
            .In(new[] { "passport", "national_id", "tax_id", "driver_license" })
                .WithMessage("DocumentType must be one of: passport, national_id, tax_id, driver_license.");

        // The same field (DocumentNumber) gets different rules depending on DocumentType
        RuleFor(x => x.DocumentNumber)
            .SwitchOn(x => x.DocumentType)
            .Case("passport", b => b
                .NotEmpty()
                    .WithMessage("Passport number is required.")
                .MinimumLength(6)
                .MaximumLength(9)
                .Matches(@"^[A-Z]{1,2}\d{6,8}$")
                    .WithMessage("Passport number must be 1-2 uppercase letters followed by 6-8 digits."))
            .Case("national_id", b => b
                .NotEmpty()
                    .WithMessage("National ID number is required.")
                .IsNumeric()
                    .WithMessage("National ID must contain only digits.")
                .MinimumLength(8)
                .MaximumLength(8)
                    .WithMessage("National ID must be exactly 8 digits."))
            .Case("tax_id", b => b
                .NotEmpty()
                    .WithMessage("Tax ID is required.")
                .IsNumeric()
                .MinimumLength(9)
                .MaximumLength(11)
                    .WithMessage("Tax ID must be between 9 and 11 digits."))
            .Case("driver_license", b => b
                .NotEmpty()
                    .WithMessage("Driver's license number is required.")
                .Matches(@"^[A-Z]{2}\d{7}$")
                    .WithMessage("Driver's license must be 2 uppercase letters followed by 7 digits."))
            .Default(b => b
                .NotEmpty()
                    .WithMessage("Document number is required."));
    }
}
```

#### Usage

```csharp
var validator = new DocumentValidator();

var passport = new DocumentDto { DocumentType = "passport", DocumentNumber = "AB1234567" };
var r1 = validator.Validate(passport);
// r1.IsValid == true

var badId = new DocumentDto { DocumentType = "national_id", DocumentNumber = "ABC" };
var r2 = validator.Validate(badId);
// r2.IsValid == false
// r2.Errors["DocumentNumber"] contains messages about numeric format and length
```

---

### Example 2 — Measurement Values with Units

When your API accepts values in multiple units, `SwitchOn` lets you express the valid range for each unit without duplicating property names.

```csharp
public class MeasurementDto
{
    public string Unit { get; set; } = string.Empty;  // "kg" | "lb" | "oz" | "g"
    public decimal Value { get; set; }
}

public class MeasurementValidator : AbstractValidator<MeasurementDto>
{
    public MeasurementValidator()
    {
        RuleFor(x => x.Unit)
            .NotEmpty()
            .In(new[] { "kg", "lb", "oz", "g" })
                .WithMessage("Unit must be one of: kg, lb, oz, g.");

        RuleFor(x => x.Value)
            .SwitchOn(x => x.Unit)
            .Case("kg", b => b
                .GreaterThan(0m)
                .LessThanOrEqualTo(1000m)
                    .WithMessage("Weight in kilograms must be between 0 and 1,000 kg."))
            .Case("lb", b => b
                .GreaterThan(0m)
                .LessThanOrEqualTo(2204m)
                    .WithMessage("Weight in pounds must be between 0 and 2,204 lb."))
            .Case("oz", b => b
                .GreaterThan(0m)
                .LessThanOrEqualTo(35274m)
                    .WithMessage("Weight in ounces must be between 0 and 35,274 oz."))
            .Case("g", b => b
                .GreaterThan(0m)
                .LessThanOrEqualTo(1000000m)
                    .WithMessage("Weight in grams must be between 0 and 1,000,000 g."))
            .Default(b => b
                .GreaterThan(0m));
    }
}
```

---

### Example 3 — Notification Channel with Body Format

```csharp
public class NotificationDto
{
    public string Channel { get; set; } = string.Empty;  // "email" | "sms" | "push"
    public string Body { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? DeviceToken { get; set; }
}

public class NotificationValidator : AbstractValidator<NotificationDto>
{
    public NotificationValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty()
            .In(new[] { "email", "sms", "push" });

        // Body has different length requirements per channel
        RuleFor(x => x.Body)
            .SwitchOn(x => x.Channel)
            .Case("email", b => b
                .NotEmpty()
                    .WithMessage("Email body cannot be empty.")
                .MaximumLength(10000)
                    .WithMessage("Email body must not exceed 10,000 characters."))
            .Case("sms", b => b
                .NotEmpty()
                    .WithMessage("SMS body cannot be empty.")
                .MaximumLength(160)
                    .WithMessage("SMS body must not exceed 160 characters."))
            .Case("push", b => b
                .NotEmpty()
                    .WithMessage("Push notification body cannot be empty.")
                .MaximumLength(256)
                    .WithMessage("Push notification body must not exceed 256 characters."))
            .Default(b => b.NotEmpty());

        // Subject is only required for email
        RuleFor(x => x.Subject)
            .NotEmpty()
                .WithMessage("Email subject is required.")
            .MaximumLength(200)
            .When(x => x.Channel == "email");

        // Recipient-specific validation using When/Unless (alternative to SwitchOn for simple cases)
        RuleFor(x => x.RecipientEmail)
            .NotEmpty()
            .Email()
            .When(x => x.Channel == "email");

        RuleFor(x => x.RecipientPhone)
            .NotEmpty()
            .PhoneNumber()
            .When(x => x.Channel == "sms");

        RuleFor(x => x.DeviceToken)
            .NotEmpty()
            .MinimumLength(32)
            .When(x => x.Channel == "push");
    }
}
```

---

### Example 4 — Enum Discriminator with SwitchOn

```csharp
public enum ProductCategory
{
    Physical,
    Digital,
    Subscription
}

public class ProductDto
{
    public ProductCategory Category { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? StockQuantity { get; set; }
    public string? DownloadUrl { get; set; }
    public string? SubscriptionPeriod { get; set; }
}

public class ProductValidator : AbstractValidator<ProductDto>
{
    public ProductValidator()
    {
        RuleFor(x => x.Price)
            .Positive()
            .MaxDecimalPlaces(2);

        // SKU format differs by category
        RuleFor(x => x.Sku)
            .SwitchOn(x => x.Category)
            .Case(ProductCategory.Physical, b => b
                .NotEmpty()
                .Matches(@"^PHY-[A-Z0-9]{6}$")
                    .WithMessage("Physical product SKU must match PHY-XXXXXX."))
            .Case(ProductCategory.Digital, b => b
                .NotEmpty()
                .Matches(@"^DIG-[A-Z0-9]{8}$")
                    .WithMessage("Digital product SKU must match DIG-XXXXXXXX."))
            .Case(ProductCategory.Subscription, b => b
                .NotEmpty()
                .Matches(@"^SUB-[A-Z0-9]{4}-[A-Z]{2}$")
                    .WithMessage("Subscription SKU must match SUB-XXXX-YY."));

        // StockQuantity makes sense only for physical products
        RuleFor(x => x.StockQuantity)
            .NotNull()
            .GreaterThanOrEqualTo(0)
            .When(x => x.Category == ProductCategory.Physical);

        RuleFor(x => x.DownloadUrl)
            .NotEmpty()
            .Url()
            .When(x => x.Category == ProductCategory.Digital);

        RuleFor(x => x.SubscriptionPeriod)
            .NotEmpty()
            .In(new[] { "monthly", "quarterly", "annual" })
            .When(x => x.Category == ProductCategory.Subscription);
    }
}
```

---

### Example 5 — Combining SwitchOn with When in the Same Builder

Rules added before the `.SwitchOn(...)` call on the same `RuleFor(...)` chain apply globally to the property (subject to their own `.When(...)` guards). Rules inside `SwitchOn` cases are conditional on the key.

```csharp
public class SalaryDto
{
    public string ContractType { get; set; } = string.Empty;  // "full_time" | "part_time" | "freelance"
    public decimal Salary { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; }
}

public class SalaryValidator : AbstractValidator<SalaryDto>
{
    public SalaryValidator()
    {
        // Salary must always be non-negative (global rule on the property)
        RuleFor(x => x.Salary)
            .NonNegative()
            .WithMessage("Salary cannot be negative.");

        // Then apply range rules that depend on contract type
        RuleFor(x => x.Salary)
            .SwitchOn(x => x.ContractType)
            .Case("full_time", b => b
                .GreaterThanOrEqualTo(20000m)
                    .WithMessage("Full-time salary must be at least 20,000.")
                .LessThanOrEqualTo(500000m)
                    .WithMessage("Full-time salary must not exceed 500,000."))
            .Case("part_time", b => b
                .GreaterThanOrEqualTo(8000m)
                    .WithMessage("Part-time salary must be at least 8,000.")
                .LessThanOrEqualTo(200000m)
                    .WithMessage("Part-time salary must not exceed 200,000."))
            .Case("freelance", b => b
                .GreaterThan(0m)
                    .WithMessage("Freelance rate must be greater than zero."));

        RuleFor(x => x.Currency)
            .NotEmpty()
            .CurrencyCode()
            .When(x => x.IsActive);
    }
}
```

---

### Example 6 — Multiple SwitchOn in One Validator

You can have as many `SwitchOn` calls as needed in a single validator, even on different properties with different discriminators.

```csharp
public class MedicalTestDto
{
    public string TestType { get; set; } = string.Empty;   // "blood" | "imaging" | "biopsy"
    public string SampleUnit { get; set; } = string.Empty; // "ml" | "mg" | "units"
    public decimal SampleValue { get; set; }
    public string ResultFormat { get; set; } = string.Empty; // "numeric" | "text" | "image_url"
    public string Result { get; set; } = string.Empty;
    public string? ReferenceRange { get; set; }
}

public class MedicalTestValidator : AbstractValidator<MedicalTestDto>
{
    public MedicalTestValidator()
    {
        // First SwitchOn: SampleValue range depends on SampleUnit
        RuleFor(x => x.SampleValue)
            .SwitchOn(x => x.SampleUnit)
            .Case("ml", b => b.Between(0.1m, 500m)
                .WithMessage("Volume in ml must be between 0.1 and 500."))
            .Case("mg", b => b.Between(0.01m, 5000m)
                .WithMessage("Mass in mg must be between 0.01 and 5,000."))
            .Case("units", b => b.Between(1m, 10000m)
                .WithMessage("Units must be between 1 and 10,000."))
            .Default(b => b.Positive());

        // Second SwitchOn: Result validation depends on ResultFormat
        RuleFor(x => x.Result)
            .SwitchOn(x => x.ResultFormat)
            .Case("numeric", b => b
                .NotEmpty()
                .IsNumeric()
                    .WithMessage("A numeric result must contain only digits."))
            .Case("text", b => b
                .NotEmpty()
                .MaximumLength(2000)
                    .WithMessage("Text result must not exceed 2,000 characters."))
            .Case("image_url", b => b
                .NotEmpty()
                .Url()
                    .WithMessage("Image URL result must be a valid URL."))
            .Default(b => b.NotEmpty());
    }
}
```

---

## Part 3: RuleSwitch vs SwitchOn vs When/Unless

### Extended Comparison Table

| Criterion | `RuleSwitch` | `SwitchOn` | `When` / `Unless` |
|---|---|---|---|
| Scope | Multiple properties | Single property | Single rule or rule group |
| Key evaluation | Once per switch block | Once per SwitchOn block | Once per condition annotation |
| Case exclusivity | Yes (first match wins) | Yes (first match wins) | No — all When/Unless are independent |
| Default branch | `.Default(...)` | `.Default(...)` | Use `.Unless(...)` as inverse |
| Composability | One per property group | Multiple per validator | Unlimited |
| Best for | Mutually exclusive schemas | Same field, different formats | Optional/conditional individual rules |
| Readability for N cases | High (named cases) | High (named cases) | Decreases with N |
| Compile-time key safety | Yes (enums) | Yes (enums) | N/A |

### When to Choose Each

**Use `RuleSwitch` when:**
- The object has fundamentally different shapes depending on a type field
- Validation involves many properties that only make sense for certain values of the discriminator
- You want named, readable "modes" that each configure a mini-validator
- Example: `OrderType` = pickup vs delivery vs digital, each requiring completely different address/token/download fields

**Use `SwitchOn` when:**
- A single property has a different format or range depending on another property
- The property is always present but its constraints vary by context
- You want to keep the `RuleFor(x => x.FieldName)` chain intact and just vary what rules apply
- Example: `DocumentNumber` format depends on `DocumentType`

**Use `When` / `Unless` when:**
- A rule is simply optional based on a boolean or simple condition
- The condition is not exhaustive (it is not a set of mutually exclusive cases)
- You need to attach a condition to a single rule, not a whole group
- Example: `MiddleName` is only validated when it is not null

### Decision Tree

```
Is the validation scope for a single property?
├── Yes
│   ├── Does the property's format/range depend on ANOTHER property's value?
│   │   ├── Yes, with multiple mutually exclusive variants → SwitchOn
│   │   └── Yes, but it is a simple binary condition → When/Unless
│   └── Is the rule simply optional/conditional?
│       └── Yes → When/Unless
└── No (multiple properties are involved)
    ├── Are the variants mutually exclusive schemas? → RuleSwitch
    └── Are most rules independent, with a few conditional additions? → When/Unless on each
```

---

## Part 4: Advanced Real-World Patterns

### Pattern 1 — E-Commerce Order by Shipping Type

```csharp
public class OrderDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string ShippingType { get; set; } = string.Empty; // "express" | "standard" | "digital"
    public List<OrderLineDto> Lines { get; set; } = new();

    // Physical shipping fields
    public string? ShippingAddress { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? TrackingCarrier { get; set; }

    // Digital delivery fields
    public string? DeliveryEmail { get; set; }
    public string? LicenseKey { get; set; }
}

public class OrderLineDto
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderLineValidator : AbstractValidator<OrderLineDto>
{
    public OrderLineValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).Positive().MaxDecimalPlaces(2);
    }
}

public class OrderValidator : AbstractValidator<OrderDto>
{
    public OrderValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .Matches(@"^ORD-\d{8}$")
                .WithMessage("Order number must match ORD-XXXXXXXX.");

        // Validate each line using RuleForEach + SetValidator
        RuleForEach(x => x.Lines).SetValidator(new OrderLineValidator());

        RuleSwitch(x => x.ShippingType)
            .Case("express", rules =>
            {
                rules.RuleFor(x => x.ShippingAddress).NotEmpty();
                rules.RuleFor(x => x.ShippingCity).NotEmpty();
                rules.RuleFor(x => x.ShippingPostalCode).NotEmpty().IsNumeric();
                rules.RuleFor(x => x.TrackingCarrier)
                    .NotEmpty()
                    .In(new[] { "fedex", "ups", "dhl" })
                        .WithMessage("Express carrier must be fedex, ups, or dhl.");
            })
            .Case("standard", rules =>
            {
                rules.RuleFor(x => x.ShippingAddress).NotEmpty();
                rules.RuleFor(x => x.ShippingCity).NotEmpty();
                rules.RuleFor(x => x.ShippingPostalCode).NotEmpty();
                // Standard does not require a specific carrier
            })
            .Case("digital", rules =>
            {
                rules.RuleFor(x => x.DeliveryEmail)
                    .NotEmpty()
                    .Email()
                        .WithMessage("A valid delivery email is required for digital orders.");

                rules.RuleFor(x => x.LicenseKey)
                    .NotEmpty()
                    .Matches(@"^[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}$")
                        .WithMessage("License key must be in XXXXX-XXXXX-XXXXX-XXXXX format.");
            })
            .Default(rules =>
            {
                rules.RuleFor(x => x.ShippingType)
                    .Must(_ => false)
                        .WithMessage("ShippingType must be 'express', 'standard', or 'digital'.");
            });
    }
}
```

---

### Pattern 2 — Multi-Step Form by Current Step

This pattern is useful for wizard-style APIs where you POST incremental data and only validate the fields relevant to the current step.

```csharp
public class RegistrationFormDto
{
    public int CurrentStep { get; set; }  // 1, 2, or 3

    // Step 1: Account credentials
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? PasswordConfirmation { get; set; }

    // Step 2: Personal information
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }

    // Step 3: Billing
    public string? BillingStreet { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? PaymentToken { get; set; }
}

public class RegistrationFormValidator : AbstractValidator<RegistrationFormDto>
{
    public RegistrationFormValidator()
    {
        RuleFor(x => x.CurrentStep)
            .Between(1, 3)
            .WithMessage("CurrentStep must be 1, 2, or 3.");

        RuleSwitch(x => x.CurrentStep)
            .Case(1, rules =>
            {
                rules.RuleFor(x => x.Email)
                    .NotEmpty()
                    .Email();

                rules.RuleFor(x => x.Password)
                    .NotEmpty()
                    .MinimumLength(8)
                    .HasUppercase()
                    .HasLowercase()
                    .HasDigit()
                    .HasSpecialChar();

                rules.RuleFor(x => x.PasswordConfirmation)
                    .NotEmpty()
                    .EqualToProperty(x => x.Password)
                        .WithMessage("Password and confirmation do not match.");
            })
            .Case(2, rules =>
            {
                rules.RuleFor(x => x.FirstName)
                    .NotEmpty()
                    .MaximumLength(100);

                rules.RuleFor(x => x.LastName)
                    .NotEmpty()
                    .MaximumLength(100);

                rules.RuleFor(x => x.DateOfBirth)
                    .NotNull()
                    .PastDate()
                        .WithMessage("Date of birth must be in the past.")
                    .MinAge(18)
                        .WithMessage("You must be at least 18 years old to register.");

                rules.RuleFor(x => x.PhoneNumber)
                    .NotEmpty()
                    .PhoneNumber();
            })
            .Case(3, rules =>
            {
                rules.RuleFor(x => x.BillingStreet).NotEmpty();
                rules.RuleFor(x => x.BillingCity).NotEmpty();
                rules.RuleFor(x => x.BillingPostalCode)
                    .NotEmpty()
                    .Matches(@"^\d{4,10}$");

                rules.RuleFor(x => x.PaymentToken)
                    .NotEmpty()
                        .WithMessage("A payment token is required to complete registration.")
                    .MinimumLength(32)
                        .WithMessage("The payment token appears to be invalid.");
            });
    }
}
```

---

### Pattern 3 — Multi-Tenant API: Customer Type (B2B/B2C/Internal)

```csharp
public class CustomerDto
{
    public string CustomerType { get; set; } = string.Empty; // "b2b" | "b2c" | "internal"
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // B2B fields
    public string? CompanyRegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? AccountManagerId { get; set; }

    // B2C fields
    public DateTime? DateOfBirth { get; set; }
    public string? LoyaltyCardNumber { get; set; }

    // Internal fields
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? CostCenter { get; set; }
}

public class CustomerValidator : AbstractValidator<CustomerDto>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().Email();

        RuleSwitch(x => x.CustomerType)
            .Case("b2b", rules =>
            {
                rules.RuleFor(x => x.CompanyRegistrationNumber)
                    .NotEmpty()
                    .Matches(@"^[A-Z0-9]{6,12}$")
                        .WithMessage("Company registration number must be 6-12 uppercase alphanumeric characters.");

                rules.RuleFor(x => x.VatNumber)
                    .NotEmpty()
                        .WithMessage("VAT number is required for B2B customers.");

                rules.RuleFor(x => x.AccountManagerId)
                    .NotEmpty()
                        .WithMessage("An account manager must be assigned to B2B customers.");
            })
            .Case("b2c", rules =>
            {
                rules.RuleFor(x => x.DateOfBirth)
                    .NotNull()
                    .PastDate()
                    .MinAge(16)
                        .WithMessage("B2C customers must be at least 16 years old.");
            })
            .Case("internal", rules =>
            {
                rules.RuleFor(x => x.EmployeeId)
                    .NotEmpty()
                    .Matches(@"^EMP-\d{5}$")
                        .WithMessage("Employee ID must match EMP-XXXXX.");

                rules.RuleFor(x => x.Department)
                    .NotEmpty()
                    .In(new[] { "engineering", "sales", "finance", "hr", "ops" })
                        .WithMessage("Department must be a known department code.");

                rules.RuleFor(x => x.CostCenter)
                    .NotEmpty()
                    .Matches(@"^CC-\d{4}$")
                        .WithMessage("Cost center must match CC-XXXX.");
            })
            .Default(rules =>
            {
                rules.RuleFor(x => x.CustomerType)
                    .Must(_ => false)
                        .WithMessage("CustomerType must be 'b2b', 'b2c', or 'internal'.");
            });
    }
}
```

---

### Pattern 4 — Scientific Measurements with Units (SwitchOn)

```csharp
public class ExperimentReadingDto
{
    public string Measurement { get; set; } = string.Empty;  // "temperature" | "pressure" | "ph"
    public string Unit { get; set; } = string.Empty;
    public double ReadingValue { get; set; }
    public string? Notes { get; set; }
}

public class ExperimentReadingValidator : AbstractValidator<ExperimentReadingDto>
{
    public ExperimentReadingValidator()
    {
        RuleFor(x => x.Unit).NotEmpty();

        // Temperature readings: valid ranges differ by unit
        RuleFor(x => x.ReadingValue)
            .SwitchOn(x => x.Unit)
            .Case("celsius", b => b
                .Between(-273.15, 10000.0)
                    .WithMessage("Temperature in Celsius must be above absolute zero (-273.15°C)."))
            .Case("fahrenheit", b => b
                .Between(-459.67, 18032.0)
                    .WithMessage("Temperature in Fahrenheit must be above absolute zero (-459.67°F)."))
            .Case("kelvin", b => b
                .GreaterThanOrEqualTo(0.0)
                    .WithMessage("Temperature in Kelvin must be non-negative."))
            .Case("bar", b => b
                .Between(0.0, 10000.0)
                    .WithMessage("Pressure in bar must be between 0 and 10,000."))
            .Case("psi", b => b
                .Between(0.0, 145000.0)
                    .WithMessage("Pressure in PSI must be between 0 and 145,000."))
            .Case("ph", b => b
                .Between(0.0, 14.0)
                    .WithMessage("pH value must be between 0 and 14."))
            .Default(b => b
                .Must(_ => true)); // Unknown units: reading is unconstrained

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes != null);
    }
}
```

---

### Pattern 5 — Financial Loan Application (RuleSwitch + Async Rules)

```csharp
public class LoanRequestDto
{
    public string LoanType { get; set; } = string.Empty;   // "personal" | "mortgage" | "vehicle" | "student"
    public string ApplicantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }

    // Mortgage
    public string? PropertyAddress { get; set; }
    public decimal? PropertyAppraisalValue { get; set; }

    // Vehicle
    public string? VehicleVin { get; set; }
    public int? VehicleModelYear { get; set; }

    // Student
    public string? InstitutionCode { get; set; }
    public string? EnrollmentProof { get; set; }
}

public class LoanRequestValidator : AbstractValidator<LoanRequestDto>
{
    private readonly IApplicantService _applicantService;

    public LoanRequestValidator(IApplicantService applicantService)
    {
        _applicantService = applicantService;

        RuleFor(x => x.ApplicantId)
            .NotEmpty()
            .MustAsync(async id =>
                await _applicantService.ExistsAsync(id))
                .WithMessage("The applicant ID does not correspond to an existing account.");

        RuleFor(x => x.Amount)
            .Positive()
            .MaxDecimalPlaces(2);

        RuleFor(x => x.TermMonths)
            .GreaterThan(0)
            .LessThanOrEqualTo(360)
                .WithMessage("Loan term may not exceed 360 months (30 years).");

        RuleSwitch(x => x.LoanType)
            .Case("personal", rules =>
            {
                rules.RuleFor(x => x.Amount)
                    .LessThanOrEqualTo(75000m)
                        .WithMessage("Personal loans may not exceed $75,000.");

                rules.RuleFor(x => x.TermMonths)
                    .LessThanOrEqualTo(84)
                        .WithMessage("Personal loan term may not exceed 84 months.");
            })
            .Case("mortgage", rules =>
            {
                rules.RuleFor(x => x.PropertyAddress)
                    .NotEmpty()
                        .WithMessage("Property address is required for mortgage applications.");

                rules.RuleFor(x => x.PropertyAppraisalValue)
                    .NotNull()
                    .Positive()
                        .WithMessage("A valid property appraisal value is required.");

                // Async rule: verify LTV ratio (amount must be <= 80% of appraisal value)
                rules.RuleFor(x => x.Amount)
                    .MustAsync(async amount =>
                    {
                        await Task.CompletedTask; // placeholder — real code calls an LTV service
                        return true;
                    })
                    .WithMessage("Loan amount exceeds the maximum loan-to-value ratio.");
            })
            .Case("vehicle", rules =>
            {
                rules.RuleFor(x => x.VehicleVin)
                    .NotEmpty()
                    .Matches(@"^[A-HJ-NPR-Z0-9]{17}$")
                        .WithMessage("Vehicle VIN must be 17 valid characters (no I, O, or Q).");

                rules.RuleFor(x => x.VehicleModelYear)
                    .NotNull()
                    .GreaterThanOrEqualTo(1980)
                    .LessThanOrEqualTo(DateTime.Now.Year + 1)
                        .WithMessage($"Vehicle model year must be between 1980 and {DateTime.Now.Year + 1}.");
            })
            .Case("student", rules =>
            {
                rules.RuleFor(x => x.InstitutionCode)
                    .NotEmpty()
                    .Matches(@"^EDU-[A-Z]{2}\d{4}$")
                        .WithMessage("Institution code must match EDU-AAXXXX.");

                rules.RuleFor(x => x.EnrollmentProof)
                    .NotEmpty()
                        .WithMessage("Proof of enrollment is required for student loans.");

                rules.RuleFor(x => x.Amount)
                    .LessThanOrEqualTo(100000m)
                        .WithMessage("Student loans may not exceed $100,000.");
            })
            .Default(rules =>
            {
                rules.RuleFor(x => x.LoanType)
                    .Must(_ => false)
                        .WithMessage("LoanType must be personal, mortgage, vehicle, or student.");
            });
    }
}

// Required interface for the async rule above
public interface IApplicantService
{
    Task<bool> ExistsAsync(string applicantId);
}
```

---

### Pattern 6 — RuleSwitch and SwitchOn Combined in One Validator

The most powerful configurations use both `RuleSwitch` (for multi-property schemas) and `SwitchOn` (for individual field formats) in the same validator.

```csharp
public class ContractDto
{
    public string ContractType { get; set; } = string.Empty;  // "fixed_price" | "time_and_material" | "retainer"
    public string BillingCycle { get; set; } = string.Empty;  // "monthly" | "quarterly" | "annual"
    public decimal Value { get; set; }
    public string? ClientReference { get; set; }
    public int? EstimatedHours { get; set; }
    public decimal? HourlyRate { get; set; }
    public int? RetainerDays { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
}

public class ContractValidator : AbstractValidator<ContractDto>
{
    public ContractValidator()
    {
        // Global rule: value is always positive
        RuleFor(x => x.Value).Positive();

        // SwitchOn: ContractNumber format depends on ContractType
        RuleFor(x => x.ContractNumber)
            .SwitchOn(x => x.ContractType)
            .Case("fixed_price", b => b
                .NotEmpty()
                .Matches(@"^FP-\d{6}-\d{4}$")
                    .WithMessage("Fixed price contract number must match FP-XXXXXX-YYYY."))
            .Case("time_and_material", b => b
                .NotEmpty()
                .Matches(@"^TM-\d{6}-\d{4}$")
                    .WithMessage("T&M contract number must match TM-XXXXXX-YYYY."))
            .Case("retainer", b => b
                .NotEmpty()
                .Matches(@"^RET-\d{8}$")
                    .WithMessage("Retainer contract number must match RET-XXXXXXXX."))
            .Default(b => b.NotEmpty());

        // SwitchOn: Value range depends on BillingCycle
        RuleFor(x => x.Value)
            .SwitchOn(x => x.BillingCycle)
            .Case("monthly", b => b
                .LessThanOrEqualTo(1000000m)
                    .WithMessage("Monthly contract value must not exceed $1,000,000."))
            .Case("quarterly", b => b
                .LessThanOrEqualTo(3000000m)
                    .WithMessage("Quarterly contract value must not exceed $3,000,000."))
            .Case("annual", b => b
                .LessThanOrEqualTo(12000000m)
                    .WithMessage("Annual contract value must not exceed $12,000,000."));

        // RuleSwitch: multi-field schema depends on ContractType
        RuleSwitch(x => x.ContractType)
            .Case("fixed_price", rules =>
            {
                rules.RuleFor(x => x.ClientReference)
                    .NotEmpty()
                        .WithMessage("A client reference is required for fixed price contracts.");
            })
            .Case("time_and_material", rules =>
            {
                rules.RuleFor(x => x.EstimatedHours)
                    .NotNull()
                    .GreaterThan(0)
                        .WithMessage("Estimated hours are required for time-and-material contracts.");

                rules.RuleFor(x => x.HourlyRate)
                    .NotNull()
                    .Positive()
                    .MaxDecimalPlaces(2)
                        .WithMessage("A valid hourly rate is required for time-and-material contracts.");
            })
            .Case("retainer", rules =>
            {
                rules.RuleFor(x => x.RetainerDays)
                    .NotNull()
                    .Between(1, 31)
                        .WithMessage("Retainer days must be between 1 and 31.");
            });
    }
}
```

---

## Part 5: Integration with ASP.NET Core, MediatR, and Vali-Mediator

Validators that use `RuleSwitch` or `SwitchOn` integrate with ASP.NET Core, MediatR, and Vali-Mediator exactly the same way as any other validator — the switch logic is entirely encapsulated inside the validator. No special configuration is required.

### ASP.NET Core (Minimal API)

```csharp
// Program.cs
builder.Services.AddValiValidation(typeof(Program).Assembly);
builder.Services.AddScoped<IApplicantService, ApplicantService>();

app.MapPost("/api/loans", async (
    LoanRequestDto dto,
    IValidator<LoanRequestDto> validator) =>
{
    var result = await validator.ValidateAsync(dto);
    if (!result.IsValid)
        return Results.ValidationProblem(
            result.Errors.ToDictionary(k => k.Key, v => v.Value.ToArray()));

    // Process loan...
    return Results.Ok();
});
```

The validator injected via `IValidator<LoanRequestDto>` is the `LoanRequestValidator`, and its `RuleSwitch` block executes transparently during `ValidateAsync`.

### ASP.NET Core (Controller)

```csharp
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IValidator<PaymentDto> _validator;

    public PaymentsController(IValidator<PaymentDto> validator)
    {
        _validator = validator;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] PaymentDto dto)
    {
        var result = await _validator.ValidateAsync(dto);
        if (!result.IsValid)
            return BadRequest(result.Errors);

        return Ok();
    }
}
```

### Vali-Mediator Pipeline Behavior

When using Vali-Mediator with `ValidationBehavior<TRequest, TResponse>`, the behavior calls `ValidateAsync` on the registered validator. If the validator uses `RuleSwitch`, the behavior triggers the correct case based on the request content automatically.

```csharp
// Program.cs
builder.Services.AddValiMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddValiValidation(typeof(Program).Assembly);
});

// Handler — the validation behavior runs before Handle is called
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<OrderId>>
{
    public async Task<Result<OrderId>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // By the time we get here, the OrderValidator (with RuleSwitch) has already run
        var id = await _orderService.CreateAsync(request);
        return Result<OrderId>.Ok(id);
    }
}
```

### MediatR Pipeline Behavior

```csharp
// Works transparently — ValidationBehavior<TRequest, TResponse> calls ValidateAsync
// The MediatR integration is unaware of RuleSwitch/SwitchOn specifics
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddValiValidation(typeof(Program).Assembly);
```

---

## Part 6: Testing Validators with Switch

Tests for switch-based validators follow the same patterns as any xUnit test for validators. The key is to cover each case explicitly plus edge cases (no match, Default, global rules).

```csharp
using Vali_Validation.Core.Validators;
using Vali_Validation.Core.Exceptions;
using Xunit;

// Models
public class PaymentTestDto
{
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? CardNumber { get; set; }
    public string? Cvv { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Iban { get; set; }
    public string? BankCode { get; set; }
    public string? PaypalEmail { get; set; }
    public string? Reference { get; set; }
}

// Minimal validator for tests
public class PaymentTestValidator : AbstractValidator<PaymentTestDto>
{
    public PaymentTestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).NotEmpty();

        RuleSwitch(x => x.Method)
            .Case("credit_card", rules =>
            {
                rules.RuleFor(x => x.CardNumber).NotEmpty().CreditCard();
                rules.RuleFor(x => x.Cvv).NotEmpty().MinimumLength(3).MaximumLength(4);
                rules.RuleFor(x => x.ExpirationDate).NotNull().FutureDate();
            })
            .Case("bank_transfer", rules =>
            {
                rules.RuleFor(x => x.Iban).NotEmpty().Iban();
                rules.RuleFor(x => x.BankCode).NotEmpty();
            })
            .Case("paypal", rules =>
            {
                rules.RuleFor(x => x.PaypalEmail).NotEmpty().Email();
            })
            .Default(rules =>
            {
                rules.RuleFor(x => x.Reference).NotEmpty();
            });
    }
}

public class PaymentValidatorTests
{
    private readonly PaymentTestValidator _validator = new PaymentTestValidator();

    // --- Global rules ---

    [Fact]
    public async Task GlobalRule_Amount_FailsWhenZero()
    {
        var dto = new PaymentTestDto { Amount = 0, Method = "paypal", PaypalEmail = "a@b.com" };
        var result = await _validator.ValidateAsync(dto);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Amount"));
    }

    [Fact]
    public async Task GlobalRule_Method_FailsWhenEmpty()
    {
        var dto = new PaymentTestDto { Amount = 10, Method = "" };
        var result = await _validator.ValidateAsync(dto);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Method"));
    }

    // --- credit_card case ---

    [Fact]
    public async Task CreditCard_WhenAllFieldsValid_Passes()
    {
        var dto = new PaymentTestDto
        {
            Amount = 99.99m,
            Method = "credit_card",
            CardNumber = "4111111111111111",
            Cvv = "123",
            ExpirationDate = DateTime.Now.AddYears(2)
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreditCard_WhenCvvMissing_ReportsCvvError()
    {
        var dto = new PaymentTestDto
        {
            Amount = 50m,
            Method = "credit_card",
            CardNumber = "4111111111111111",
            Cvv = null,
            ExpirationDate = DateTime.Now.AddYears(1)
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Cvv"));
        // Fields for other cases must NOT appear
        Assert.False(result.HasErrorFor("Iban"));
        Assert.False(result.HasErrorFor("PaypalEmail"));
    }

    [Fact]
    public async Task CreditCard_WhenExpirationInPast_ReportsExpirationError()
    {
        var dto = new PaymentTestDto
        {
            Amount = 100m,
            Method = "credit_card",
            CardNumber = "4111111111111111",
            Cvv = "456",
            ExpirationDate = DateTime.Now.AddYears(-1)  // expired
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("ExpirationDate"));
    }

    // --- bank_transfer case ---

    [Fact]
    public async Task BankTransfer_WhenAllFieldsValid_Passes()
    {
        var dto = new PaymentTestDto
        {
            Amount = 1000m,
            Method = "bank_transfer",
            Iban = "GB29NWBK60161331926819",
            BankCode = "NWBK"
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task BankTransfer_WhenIbanMissing_ReportsIbanError()
    {
        var dto = new PaymentTestDto
        {
            Amount = 500m,
            Method = "bank_transfer",
            Iban = null,
            BankCode = "NWBK"
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Iban"));
        // Credit card fields must NOT appear
        Assert.False(result.HasErrorFor("CardNumber"));
    }

    // --- paypal case ---

    [Fact]
    public async Task PayPal_WhenEmailInvalid_ReportsPaypalEmailError()
    {
        var dto = new PaymentTestDto
        {
            Amount = 25m,
            Method = "paypal",
            PaypalEmail = "not-an-email"
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("PaypalEmail"));
    }

    // --- Default case ---

    [Fact]
    public async Task UnknownMethod_DefaultCase_RequiresReference()
    {
        var dto = new PaymentTestDto
        {
            Amount = 10m,
            Method = "crypto",       // does not match any case
            Reference = null          // required by Default
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Reference"));
    }

    [Fact]
    public async Task UnknownMethod_DefaultCase_PassesWhenReferencePresent()
    {
        var dto = new PaymentTestDto
        {
            Amount = 10m,
            Method = "crypto",
            Reference = "REF-2024-001"
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    // --- Case exclusivity ---

    [Fact]
    public async Task CreditCard_DoesNotValidateBankTransferFields()
    {
        // Even if Iban/BankCode are empty, they should not generate errors when Method is credit_card
        var dto = new PaymentTestDto
        {
            Amount = 50m,
            Method = "credit_card",
            CardNumber = "4111111111111111",
            Cvv = "123",
            ExpirationDate = DateTime.Now.AddYears(1),
            Iban = null,      // empty, but should not cause an error
            BankCode = null   // same
        };

        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
        Assert.False(result.HasErrorFor("Iban"));
    }
}

// --- SwitchOn tests ---

public class DocumentTestDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
}

public class DocumentTestValidator : AbstractValidator<DocumentTestDto>
{
    public DocumentTestValidator()
    {
        RuleFor(x => x.DocumentNumber)
            .SwitchOn(x => x.DocumentType)
            .Case("passport", b => b.NotEmpty().Matches(@"^[A-Z]{1,2}\d{6,8}$"))
            .Case("national_id", b => b.NotEmpty().IsNumeric().MinimumLength(8).MaximumLength(8))
            .Default(b => b.NotEmpty());
    }
}

public class DocumentValidatorTests
{
    private readonly DocumentTestValidator _validator = new DocumentTestValidator();

    [Theory]
    [InlineData("AB1234567", true)]
    [InlineData("A123456", true)]
    [InlineData("123456", false)]   // no letters prefix
    [InlineData("", false)]         // empty
    public async Task Passport_DocumentNumber_ValidatesFormat(string number, bool expected)
    {
        var dto = new DocumentTestDto { DocumentType = "passport", DocumentNumber = number };
        var result = await _validator.ValidateAsync(dto);
        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData("12345678", true)]   // exactly 8 numeric digits
    [InlineData("1234567", false)]   // too short
    [InlineData("123456789", false)] // too long
    [InlineData("ABCDEFGH", false)]  // not numeric
    public async Task NationalId_DocumentNumber_ValidatesFormat(string number, bool expected)
    {
        var dto = new DocumentTestDto { DocumentType = "national_id", DocumentNumber = number };
        var result = await _validator.ValidateAsync(dto);
        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public async Task UnknownDocumentType_DefaultCase_FailsWhenEmpty()
    {
        var dto = new DocumentTestDto { DocumentType = "other", DocumentNumber = "" };
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("DocumentNumber"));
    }

    [Fact]
    public async Task UnknownDocumentType_DefaultCase_PassesWhenNotEmpty()
    {
        var dto = new DocumentTestDto { DocumentType = "other", DocumentNumber = "SOME-VALUE" };
        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }
}
```

---

## Part 7: Common Mistakes and How to Avoid Them

### Mistake 1 — Using When/Unless Instead of SwitchOn for Mutually Exclusive Formats

**Wrong:**

```csharp
// Anti-pattern: two independent When conditions that overlap logic
RuleFor(x => x.DocumentNumber)
    .Matches(@"^[A-Z]{2}\d{6,7}$")
    .When(x => x.DocumentType == "passport");

RuleFor(x => x.DocumentNumber)
    .IsNumeric().MinimumLength(8).MaximumLength(8)
    .When(x => x.DocumentType == "national_id");

// Problem: both conditions are independently evaluated.
// If DocumentType is "passport", the second chain's When condition is false,
// so it is silently skipped — which is correct. But if you add a third document type,
// you need another RuleFor call, and you duplicate the property name string.
// There is no Default, no exhaustiveness, and the structure does not communicate
// that these cases are mutually exclusive.
```

**Correct:**

```csharp
// Single RuleFor, SwitchOn makes the exclusivity explicit
RuleFor(x => x.DocumentNumber)
    .SwitchOn(x => x.DocumentType)
    .Case("passport", b => b.NotEmpty().Matches(@"^[A-Z]{2}\d{6,7}$"))
    .Case("national_id", b => b.NotEmpty().IsNumeric().MinimumLength(8).MaximumLength(8))
    .Default(b => b.NotEmpty());
```

---

### Mistake 2 — Putting Global Rules Inside the Switch

**Wrong:**

```csharp
// Anti-pattern: Amount validation duplicated in every case
RuleSwitch(x => x.Method)
    .Case("credit_card", rules =>
    {
        rules.RuleFor(x => x.Amount).GreaterThan(0); // duplicated
        rules.RuleFor(x => x.CardNumber).NotEmpty();
    })
    .Case("paypal", rules =>
    {
        rules.RuleFor(x => x.Amount).GreaterThan(0); // duplicated again
        rules.RuleFor(x => x.PaypalEmail).NotEmpty().Email();
    });
```

**Correct:**

```csharp
// Global rules go outside the switch — they always run
RuleFor(x => x.Amount).GreaterThan(0);

RuleSwitch(x => x.Method)
    .Case("credit_card", rules => rules.RuleFor(x => x.CardNumber).NotEmpty())
    .Case("paypal", rules => rules.RuleFor(x => x.PaypalEmail).NotEmpty().Email());
```

---

### Mistake 3 — Expecting Both Cases to Run

**Wrong assumption:**

```csharp
// Developer expects BOTH cases to run and accumulate errors
RuleSwitch(x => x.Type)
    .Case("A", rules => rules.RuleFor(x => x.FieldA).NotEmpty())
    .Case("A", rules => rules.RuleFor(x => x.FieldB).NotEmpty()); // duplicate key!
// Only the FIRST "A" case executes — FieldB is never validated
```

**Correct:**

```csharp
// Put all rules for the same key in a single Case block
RuleSwitch(x => x.Type)
    .Case("A", rules =>
    {
        rules.RuleFor(x => x.FieldA).NotEmpty();
        rules.RuleFor(x => x.FieldB).NotEmpty();  // both in the same block
    });
```

---

### Mistake 4 — Calling ValidateAsync When the Switch Has Async Rules but Using Validate

Async rules inside a `RuleSwitch` or `SwitchOn` case only execute during `ValidateAsync`. If you call the synchronous `Validate(instance)`, async rules are silently skipped.

**Wrong:**

```csharp
// The validator has MustAsync inside a Case block
var result = validator.Validate(dto);  // async rules are NOT executed
```

**Correct:**

```csharp
// Always use ValidateAsync when your validator has any async rules
var result = await validator.ValidateAsync(dto);
```

---

### Mistake 5 — Not Handling the Default Case for Unknown Values

When the discriminator comes from user input (a string field in an API request body), unexpected values produce no errors from the switch block unless a Default is defined. This means an unknown method silently passes validation.

**Wrong:**

```csharp
// If Method = "bitcoin", no errors are reported from the switch
RuleSwitch(x => x.Method)
    .Case("credit_card", rules => { /* ... */ })
    .Case("paypal", rules => { /* ... */ });
// Method = "bitcoin" → switch block produces zero errors
```

**Correct:**

```csharp
RuleSwitch(x => x.Method)
    .Case("credit_card", rules => { /* ... */ })
    .Case("paypal", rules => { /* ... */ })
    .Default(rules =>
    {
        rules.RuleFor(x => x.Method)
            .Must(_ => false)
            .WithMessage("Method must be 'credit_card' or 'paypal'.");
    });

// Or use an In rule globally, before the switch:
RuleFor(x => x.Method)
    .In(new[] { "credit_card", "paypal" })
    .WithMessage("Method must be 'credit_card' or 'paypal'.");
```

---

### Mistake 6 — Using RuleSwitch When When/Unless Would Be Simpler

`RuleSwitch` has a registration cost (it creates delegate closures). For a single optional field, `When` is simpler and just as correct.

**Unnecessary:**

```csharp
// Overkill: switch with a single boolean-like case and a Default
RuleSwitch(x => x.IsVip)
    .Case(true, rules => rules.RuleFor(x => x.VipCode).NotEmpty())
    .Default(rules => { /* nothing */ });
```

**Simpler:**

```csharp
RuleFor(x => x.VipCode)
    .NotEmpty()
    .When(x => x.IsVip);
```

Use `RuleSwitch` when you have three or more distinct schemas, or when naming the cases improves readability significantly.

---

## Next Steps

- [07 — Modifiers](07-modifiers.md): `WithMessage`, `WithErrorCode`, `OverridePropertyName`, `StopOnFirstFailure`
- [08 — CascadeMode](08-cascade-mode.md): stopping after the first failure at property or validator level
- [15 — Advanced Patterns](15-advanced-patterns.md): `SetValidator`, `RuleForEach`, `Include`, nested validators
- [12 — ASP.NET Core Integration](12-aspnetcore-integration.md): filter, middleware, and DI registration
- [13 — MediatR Integration](13-mediatr-integration.md): `ValidationBehavior<TRequest, TResponse>` with MediatR
- [14 — Vali-Mediator Integration](14-valimediator-integration.md): `AddValiValidation` pipeline behavior
