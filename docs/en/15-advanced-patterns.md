# Advanced Patterns

This document covers complex use cases: validator composition, inheritance, advanced conditional validation, passwords and nested collections.

---

## Nested Validators with SetValidator

### Model with Multiple Nesting Levels

```csharp
public class CreateInvoiceRequest
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public CustomerInfo Customer { get; set; } = new();
    public Address BillingAddress { get; set; } = new();
    public Address ShippingAddress { get; set; } = new();
    public List<InvoiceLine> Lines { get; set; } = new();
    public PaymentInfo Payment { get; set; } = new();
}

public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class InvoiceLine
{
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
}

public class PaymentInfo
{
    public string Method { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string? CardLastFour { get; set; }
}
```

### Validators per Section

```csharp
public class CustomerInfoValidator : AbstractValidator<CustomerInfo>
{
    public CustomerInfoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TaxId)
            .NotEmpty()
            .Matches(@"^[A-Z0-9]{9}$")
                .WithMessage("The tax ID must have 9 uppercase alphanumeric characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email();
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .Matches(@"^\d{5}$")
                .WithMessage("The postal code must have 5 digits.");

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .LengthBetween(2, 3)
            .Uppercase()
                .WithMessage("The country code must be uppercase (e.g.: US, FR, DE).");
    }
}

public class InvoiceLineValidator : AbstractValidator<InvoiceLine>
{
    public InvoiceLineValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThan(0m).MaxDecimalPlaces(2);

        RuleFor(x => x.DiscountPercent)
            .ExclusiveBetween(0m, 100m)
                .WithMessage("The discount must be a percentage between 0 and 100 (exclusive).")
            .When(x => x.DiscountPercent.HasValue);
    }
}

public class PaymentInfoValidator : AbstractValidator<PaymentInfo>
{
    private static readonly string[] ValidMethods = new[] { "card", "transfer", "cash" };

    public PaymentInfoValidator()
    {
        RuleFor(x => x.Method)
            .NotEmpty()
            .In(ValidMethods)
                .WithMessage("The payment method must be: card, transfer or cash.");

        RuleFor(x => x.Iban)
            .NotEmpty()
                .WithMessage("The IBAN is required for bank transfers.")
            .Matches(@"^[A-Z]{2}\d{2}[A-Z0-9]{4}\d{14}$")
                .WithMessage("The IBAN does not have a valid format.")
            .When(x => x.Method == "transfer");

        RuleFor(x => x.CardLastFour)
            .NotEmpty()
                .WithMessage("The last 4 digits are required for card payments.")
            .Matches(@"^\d{4}$")
                .WithMessage("Must be exactly 4 digits.")
            .When(x => x.Method == "card");
    }
}
```

### Root Validator That Composes Everything

```csharp
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty()
            .Matches(@"^INV-\d{4}-\d{6}$")
                .WithMessage("The invoice number must have the format INV-YYYY-XXXXXX.");

        RuleFor(x => x.Customer)
            .NotNull()
            .SetValidator(new CustomerInfoValidator());
        // Errors: Customer.Name, Customer.TaxId, Customer.Email

        RuleFor(x => x.BillingAddress)
            .NotNull()
            .SetValidator(new AddressValidator());
        // Errors: BillingAddress.Street, BillingAddress.City, etc.

        RuleFor(x => x.ShippingAddress)
            .SetValidator(new AddressValidator())
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.Lines)
            .NotEmptyCollection()
                .WithMessage("The invoice must have at least one line.");

        RuleForEach(x => x.Lines)
            .SetValidator(new InvoiceLineValidator());
        // Errors: Lines[0].ProductCode, Lines[1].UnitPrice, etc.

        RuleFor(x => x.Payment)
            .NotNull()
            .SetValidator(new PaymentInfoValidator());
    }
}
```

---

## Include for Validator Inheritance

`Include` allows building validators in layers, where each layer adds rules without knowing the rules of the others.

### Case: Domain Entities with Common Fields

```csharp
// Interface that all auditable entities implement
public interface IAuditableEntity
{
    string CreatedBy { get; }
    DateTime CreatedAt { get; }
}

// Creation request (no audit, set by the system)
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Admin update request (with manual audit)
public class AdminUpdateProductRequest : CreateProductRequest
{
    public int Id { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public string ModificationReason { get; set; } = string.Empty;
}
```

```csharp
// Base validator for common fields shared by create and update
public class ProductBaseValidator : AbstractValidator<CreateProductRequest>
{
    public ProductBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0m)
            .MaxDecimalPlaces(2);
    }
}

// Specific creation validator
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        Include(new ProductBaseValidator());
        // Only the ProductBaseValidator rules, nothing else
    }
}

// Admin update validator — extends the base rules
public class AdminUpdateProductValidator : AbstractValidator<AdminUpdateProductRequest>
{
    public AdminUpdateProductValidator()
    {
        // Includes base rules from CreateProductRequest
        Include(new ProductBaseValidator());

        // Adds update-specific rules
        RuleFor(x => x.Id)
            .GreaterThan(0)
                .WithMessage("The product ID must be valid.");

        RuleFor(x => x.ModifiedBy)
            .NotEmpty()
                .WithMessage("The name of the modifying admin is required.");

        RuleFor(x => x.ModificationReason)
            .NotEmpty()
                .WithMessage("A reason for the modification is required.")
            .MinimumLength(20)
                .WithMessage("The reason must have at least 20 characters.")
            .MaximumLength(500);
    }
}
```

### Case: Multi-Tenant Validators with Common Rules

```csharp
// Common rules for all tenants
public class BaseUserValidator : AbstractValidator<UserRequest>
{
    protected BaseUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().Email();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

// Additional rules for Tenant A (mandatory 2FA verification)
public class TenantAUserValidator : AbstractValidator<UserRequest>
{
    public TenantAUserValidator()
    {
        Include(new BaseUserValidator());

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
                .WithMessage("Tenant A requires a phone number for 2FA.")
            .PhoneNumber();

        RuleFor(x => x.Department)
            .NotEmpty()
                .WithMessage("Tenant A requires the department to be specified.");
    }
}

// Additional rules for Tenant B (no extra restrictions)
public class TenantBUserValidator : AbstractValidator<UserRequest>
{
    public TenantBUserValidator()
    {
        Include(new BaseUserValidator());
        // Only the base rules
    }
}
```

---

## Complex Conditional Validation

### Pattern: Entity Type Determines Required Fields

```csharp
public class CreateCustomerRequest
{
    public string CustomerType { get; set; } = string.Empty; // "individual" | "company"

    // Fields for individuals
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? NationalId { get; set; }

    // Fields for companies
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? LegalRepresentative { get; set; }

    // Common fields
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        // Type is always required
        RuleFor(x => x.CustomerType)
            .NotEmpty()
            .In(new[] { "individual", "company" })
                .WithMessage("The customer type must be 'individual' or 'company'.")
            .StopOnFirstFailure();

        // Common fields
        RuleFor(x => x.Email).NotEmpty().Email();
        RuleFor(x => x.Phone).NotEmpty().PhoneNumber();

        // Individual fields — only if individual
        RuleFor(x => x.FirstName)
            .NotEmpty()
                .WithMessage("The first name is required for individual customers.")
            .MaximumLength(100)
            .When(x => x.CustomerType == "individual");

        RuleFor(x => x.LastName)
            .NotEmpty()
                .WithMessage("The last name is required for individual customers.")
            .MaximumLength(100)
            .When(x => x.CustomerType == "individual");

        RuleFor(x => x.BirthDate)
            .NotNull()
                .WithMessage("The birth date is required for individual customers.")
            .PastDate()
            .Must(d => d.HasValue && DateTime.Today.Year - d.Value.Year >= 18)
                .WithMessage("The customer must be of legal age.")
            .When(x => x.CustomerType == "individual");

        RuleFor(x => x.NationalId)
            .NotEmpty()
                .WithMessage("The national ID is required for individual customers.")
            .Matches(@"^[0-9]{8}[A-Z]$|^[XYZ][0-9]{7}[A-Z]$")
                .WithMessage("The national ID does not have a valid format.")
            .When(x => x.CustomerType == "individual");

        // Company fields — only if company
        RuleFor(x => x.CompanyName)
            .NotEmpty()
                .WithMessage("The company name is required for companies.")
            .MaximumLength(300)
            .When(x => x.CustomerType == "company");

        RuleFor(x => x.TaxId)
            .NotEmpty()
                .WithMessage("The tax ID is required for companies.")
            .Matches(@"^[A-Z][0-9]{7}[A-Z0-9]$")
                .WithMessage("The tax ID does not have a valid format.")
            .When(x => x.CustomerType == "company");

        RuleFor(x => x.LegalRepresentative)
            .NotEmpty()
                .WithMessage("The legal representative is required for companies.")
            .When(x => x.CustomerType == "company");
    }
}
```

### Pattern: Validation Dependent on Order Context

```csharp
public class ShippingOptionsRequest
{
    public string DeliveryType { get; set; } = string.Empty; // "standard", "express", "pickup"

    public string? PickupLocationId { get; set; }     // only for pickup
    public DateTime? PreferredDeliveryDate { get; set; } // only for express
    public decimal? InsuredValue { get; set; }         // optional for all

    public Address? DeliveryAddress { get; set; }      // required for standard/express
}

public class ShippingOptionsValidator : AbstractValidator<ShippingOptionsRequest>
{
    private static readonly string[] ValidTypes = new[] { "standard", "express", "pickup" };

    public ShippingOptionsValidator()
    {
        RuleFor(x => x.DeliveryType)
            .NotEmpty()
            .In(ValidTypes)
            .StopOnFirstFailure();

        // For pickup: needs pickup location
        RuleFor(x => x.PickupLocationId)
            .NotEmpty()
                .WithMessage("You must select a pickup location.")
            .When(x => x.DeliveryType == "pickup");

        // For standard/express: needs delivery address
        RuleFor(x => x.DeliveryAddress)
            .NotNull()
                .WithMessage("The delivery address is required.")
            .SetValidator(new AddressValidator())
            .When(x => x.DeliveryType is "standard" or "express");

        // For express: can specify preferred date
        RuleFor(x => x.PreferredDeliveryDate)
            .FutureDate()
                .WithMessage("The preferred delivery date must be in the future.")
            .Must(d => d.HasValue && (d.Value - DateTime.Today).TotalDays <= 30)
                .WithMessage("The preferred date cannot be more than 30 days in the future.")
            .When(x => x.DeliveryType == "express" && x.PreferredDeliveryDate.HasValue);

        // Insured value: always optional, but if specified must be positive
        RuleFor(x => x.InsuredValue)
            .GreaterThan(0m)
                .WithMessage("The insured value must be greater than 0.")
            .LessThanOrEqualTo(50000m)
                .WithMessage("The maximum insured value is 50,000.")
            .When(x => x.InsuredValue.HasValue);
    }
}
```

---

## Advanced Password Validation

```csharp
public class PasswordPolicyValidator : AbstractValidator<SetPasswordRequest>
{
    private static readonly string[] CommonPasswords = new[]
    {
        "password", "12345678", "qwerty123", "admin1234", "letmein!"
    };

    public PasswordPolicyValidator()
    {
        RuleFor(x => x.NewPassword)
            // Basic structure
            .NotEmpty()
                .WithMessage("The password is required.")
            .MinimumLength(12)
                .WithMessage("The password must have at least 12 characters.")
            .MaximumLength(128)
                .WithMessage("The password cannot exceed 128 characters.")
            // Complexity
            .HasUppercase()
                .WithMessage("Must contain at least one uppercase letter.")
            .HasLowercase()
                .WithMessage("Must contain at least one lowercase letter.")
            .HasDigit()
                .WithMessage("Must contain at least one number.")
            .HasSpecialChar()
                .WithMessage("Must contain at least one special character (! @ # $ % ...).")
            // Prohibited passwords
            .Must(pwd => !CommonPasswords.Any(common =>
                pwd.Contains(common, StringComparison.OrdinalIgnoreCase)))
                .WithMessage("The password contains a sequence that is too common.")
                .WithErrorCode("PASSWORD_TOO_COMMON")
            // No spaces
            .NoWhitespace()
                .WithMessage("The password cannot contain spaces.")
            .StopOnFirstFailure();

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .EqualToProperty(x => x.NewPassword)
                .WithMessage("The passwords do not match.")
            .StopOnFirstFailure();
    }
}
```

---

## Complex Nested Collections

### Category and Product Tree

```csharp
public class ImportCatalogRequest
{
    public List<CategoryNode> Categories { get; set; } = new();
}

public class CategoryNode
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ProductNode> Products { get; set; } = new();
    public List<CategoryNode> Subcategories { get; set; } = new();
}

public class ProductNode
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<string> Images { get; set; } = new();
}
```

```csharp
public class ProductNodeValidator : AbstractValidator<ProductNode>
{
    public ProductNodeValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .Matches(@"^[A-Z0-9-]{3,20}$")
                .WithMessage("The SKU must have between 3 and 20 alphanumeric characters or hyphens.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Price).GreaterThan(0m).MaxDecimalPlaces(2);

        RuleFor(x => x.Images)
            .MaxCount(10)
                .WithMessage("A product can have at most 10 images.");

        RuleForEach(x => x.Images)
            .NotEmpty()
            .Url()
                .WithMessage("Each image must be a valid URL.");
    }
}

public class CategoryNodeValidator : AbstractValidator<CategoryNode>
{
    public CategoryNodeValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^[A-Z0-9_]{2,20}$")
                .WithMessage("The category code must be uppercase alphanumeric.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        // Validates each product within the category
        RuleForEach(x => x.Products)
            .SetValidator(new ProductNodeValidator());

        // Validates subcategories recursively — maximum 1 level deep
        RuleFor(x => x.Subcategories)
            .MaxCount(20)
                .WithMessage("A category can have at most 20 subcategories.");

        RuleForEach(x => x.Subcategories)
            .Must(sub => sub.Subcategories.Count == 0)
                .WithMessage("Subcategories cannot have nested subcategories.")
            .SetValidator(new CategoryNodeValidator());
    }
}

public class ImportCatalogValidator : AbstractValidator<ImportCatalogRequest>
{
    public ImportCatalogValidator()
    {
        RuleFor(x => x.Categories)
            .NotEmptyCollection()
                .WithMessage("The catalog must have at least one category.")
            .MaxCount(100)
                .WithMessage("The catalog cannot have more than 100 root categories.");

        RuleForEach(x => x.Categories)
            .SetValidator(new CategoryNodeValidator());
    }
}
```

---

## Validator with Calculated State

In some cases you need to calculate a value once and use it in multiple rules. You can do this in the validator constructor:

```csharp
public class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionValidator(ISubscriptionPlanRepository plans)
    {
        // You cannot make async calls here, but you can inject
        // services for use in MustAsync

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .MustAsync(async (req, planId, ct) =>
            {
                var plan = await plans.GetByIdAsync(planId, ct);
                return plan != null && plan.IsActive;
            })
            .WithMessage("The subscription plan does not exist or is not active.")
            .StopOnFirstFailure();

        RuleFor(x => x.BillingCycle)
            .NotEmpty()
            .In(new[] { "monthly", "annual" })
                .WithMessage("The billing cycle must be 'monthly' or 'annual'.");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty()
            .MustAsync(async (req, pmId, ct) =>
            {
                var plan = await plans.GetByIdAsync(req.PlanId, ct);
                if (plan == null) return true; // Already validated above

                // Free plan does not require a payment method
                return plan.Price == 0 || !string.IsNullOrEmpty(pmId);
            })
            .WithMessage("Paid plans require a registered payment method.")
            .WhenAsync(async (req, ct) =>
            {
                // Only validate if the plan exists
                return await plans.ExistsAsync(req.PlanId, ct);
            });
    }
}
```

---

## Rule Reuse with Extension Methods

You can create extensions for `IRuleBuilder` that encapsulate rules common to multiple validators:

```csharp
public static class ValiValidationExtensions
{
    // Validation for a national identification number
    public static IRuleBuilder<T, string> IsValidNationalId<T>(
        this IRuleBuilder<T, string> builder)
    {
        return builder
            .NotEmpty()
            .Matches(@"^\d{9}$")
                .WithMessage("The identification document does not have a valid format.");
    }

    // Validation for IBAN
    public static IRuleBuilder<T, string> IsValidIban<T>(
        this IRuleBuilder<T, string> builder)
    {
        return builder
            .NotEmpty()
            .Matches(@"^[A-Z]{2}\d{2}[A-Z0-9]+$")
                .WithMessage("The IBAN does not have a valid format.")
            .MinimumLength(15)
            .MaximumLength(34);
    }

    // Price validation with currency
    public static IRuleBuilder<T, decimal> IsValidPrice<T>(
        this IRuleBuilder<T, decimal> builder,
        decimal maxPrice = 999999.99m)
    {
        return builder
            .GreaterThan(0m)
                .WithMessage("The price must be greater than 0.")
            .LessThanOrEqualTo(maxPrice)
                .WithMessage($"The price cannot exceed {maxPrice:C}.")
            .MaxDecimalPlaces(2)
                .WithMessage("The price cannot have more than 2 decimal places.");
    }
}

// Usage in validators
public class BankAccountValidator : AbstractValidator<BankAccount>
{
    public BankAccountValidator()
    {
        RuleFor(x => x.HolderNationalId).IsValidNationalId();
        RuleFor(x => x.Iban).IsValidIban();
    }
}

public class ProductCatalogEntryValidator : AbstractValidator<ProductCatalogEntry>
{
    public ProductCatalogEntryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).IsValidPrice(maxPrice: 50000m);
        RuleFor(x => x.CostPrice).IsValidPrice(maxPrice: 25000m);
    }
}
```

---

## Combining New Rules in Real Scenarios

The sections below show how to put several of the newer rules together in cohesive validators for common application domains.

### Booking Form: Date Ranges and Guest Counts

A hotel or event booking form must ensure that the reservation window is valid, the dates fall on working days and the guest count is positive.

```csharp
public class CreateBookingRequest
{
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Guests { get; set; }
    public string? SpecialRequests { get; set; }
}

public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.CheckInDate)
            .FutureDate()
                .WithMessage("The check-in date must be in the future.")
            .IsWeekday()
                .WithMessage("Check-in is only available Monday–Friday.")
            .WithinNext(TimeSpan.FromDays(180))
                .WithMessage("Bookings can only be made up to 6 months in advance.");

        RuleFor(x => x.CheckOutDate)
            .GreaterThanProperty(x => x.CheckInDate)
                .WithMessage("The check-out date must be after the check-in date.")
            .DateBetween(DateTime.Today, DateTime.Today.AddDays(365))
                .WithMessage("The check-out date must be within the next year.");

        RuleFor(x => x.Guests)
            .Positive()
                .WithMessage("There must be at least one guest.");

        RuleFor(x => x.SpecialRequests)
            .MaximumLength(500)
            .NoHtmlTags()
                .WithMessage("Special requests cannot contain HTML.")
            .When(x => x.SpecialRequests != null);
    }
}
```

### User Registration: Age, Password Policy, and Country

A user registration form that must enforce a minimum age, a strong password and a valid country of residence.

```csharp
public class RegisterUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MinWords(2)
                .WithMessage("Please enter your first and last name.")
            .MaximumLength(200)
            .NoHtmlTags();

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
            .MaximumLength(320);

        RuleFor(x => x.BirthDate)
            .PastDate()
                .WithMessage("The birth date must be in the past.")
            .MinAge(18)
                .WithMessage("You must be at least 18 years old to register.")
            .MaxAge(120)
                .WithMessage("Please enter a realistic birth date.");

        // All-in-one password policy: 10+ chars, upper, lower, digit, special
        RuleFor(x => x.Password)
            .NotEmpty()
            .PasswordPolicy(minLength: 10, requireSpecialChar: true)
                .WithMessage("The password does not meet the security policy.")
            .StopOnFirstFailure();

        RuleFor(x => x.ConfirmPassword)
            .EqualToProperty(x => x.Password)
                .WithMessage("The passwords do not match.");

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .CountryCode()
                .WithMessage("Please select a valid country (ISO 3166-1 alpha-2 code).");
    }
}
```

### Financial Form: Precision, Percentage, and IBAN

A payment or investment form that combines decimal precision constraints, a percentage fee and a bank account number.

```csharp
public class CreatePaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string BeneficiaryIban { get; set; } = string.Empty;
    public decimal ServiceFeePercent { get; set; }
    public string? Metadata { get; set; }
}

public class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.Amount)
            .Positive()
                .WithMessage("The payment amount must be greater than 0.")
            // Stored as DECIMAL(18, 2) in the database
            .Precision(18, 2)
                .WithMessage("The amount cannot have more than 2 decimal places.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .CurrencyCode()
                .WithMessage("The currency must be a valid ISO 4217 code (e.g. USD, EUR, GBP).");

        RuleFor(x => x.BeneficiaryIban)
            .NotEmpty()
                .WithMessage("The beneficiary IBAN is required.")
            .Iban()
                .WithMessage("The beneficiary IBAN is not valid.");

        RuleFor(x => x.ServiceFeePercent)
            .NonNegative()
                .WithMessage("The service fee cannot be negative.")
            .Percentage()
                .WithMessage("The service fee must be between 0% and 100%.")
            .Precision(5, 2)
                .WithMessage("The service fee cannot have more than 2 decimal places.");

        RuleFor(x => x.Metadata)
            .IsValidJson()
                .WithMessage("The metadata field must contain valid JSON.")
            .When(x => x.Metadata != null);
    }
}
```

### API Request with Structured Metadata Field

Some APIs accept a freeform `metadata` string (e.g. a JSON object or Base64-encoded payload). The following pattern validates both cases depending on a content-type discriminator.

```csharp
public class IngestEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string PayloadEncoding { get; set; } = "json"; // "json" | "base64"
    public string? SourceIp { get; set; }
    public string? MacAddress { get; set; }
}

public class IngestEventValidator : AbstractValidator<IngestEventRequest>
{
    public IngestEventValidator()
    {
        RuleFor(x => x.EventType)
            .NotEmpty()
            .IsAlphanumeric()
            .MaximumLength(100);

        RuleFor(x => x.Payload)
            .NotEmpty()
            .IsValidJson()
                .WithMessage("The payload must be valid JSON.")
            .When(x => x.PayloadEncoding == "json");

        RuleFor(x => x.Payload)
            .NotEmpty()
            .IsValidBase64()
                .WithMessage("The payload must be valid Base64.")
            .When(x => x.PayloadEncoding == "base64");

        RuleFor(x => x.SourceIp)
            .IPv4()
                .WithMessage("The source IP must be a valid IPv4 address.")
            .When(x => x.SourceIp != null && !x.SourceIp.Contains(':'));

        RuleFor(x => x.SourceIp)
            .IPv6()
                .WithMessage("The source IP must be a valid IPv6 address.")
            .When(x => x.SourceIp != null && x.SourceIp.Contains(':'));

        RuleFor(x => x.MacAddress)
            .MacAddress()
                .WithMessage("The MAC address is not valid.")
            .When(x => x.MacAddress != null);
    }
}
```

---

## Switch/Case: Polymorphic Validation

When your object has a **type discriminator** field that determines which variant of the object you are dealing with, `RuleSwitch` and `SwitchOn` give you a clean, exhaustive way to express that branching logic. The following scenarios show how they work together in real applications.

### Scenario 1: Shipping Form with Multiple Delivery Types

A shipping request can be routed to a home address, collected at a store, or dropped at a locker. Each mode requires a completely different set of fields.

```csharp
public class ShipmentRequest
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string ShippingType { get; set; } = string.Empty; // "home_delivery" | "store_pickup" | "locker"

    // home_delivery
    public string? RecipientName { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    // store_pickup
    public string? StoreCode { get; set; }
    public DateTime? PickupWindow { get; set; }

    // locker
    public string? LockerId { get; set; }
    public string? LockerAccessCode { get; set; }
}

public class ShipmentValidator : AbstractValidator<ShipmentRequest>
{
    public ShipmentValidator()
    {
        // Always required: the tracking number and a valid type
        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .Matches(@"^TRK-\d{10}$")
                .WithMessage("The tracking number must have the format TRK-XXXXXXXXXX.");

        RuleFor(x => x.ShippingType)
            .NotEmpty()
            .In(new[] { "home_delivery", "store_pickup", "locker" })
                .WithMessage("The shipping type must be home_delivery, store_pickup or locker.")
            .StopOnFirstFailure();

        // Each delivery mode activates its own set of rules
        RuleSwitch(x => x.ShippingType)
            .Case("home_delivery", rules =>
            {
                rules.RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(200);
                rules.RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
                rules.RuleFor(x => x.City).NotEmpty().MaximumLength(100);
                rules.RuleFor(x => x.PostalCode)
                    .NotEmpty()
                    .Matches(@"^\d{5}$")
                        .WithMessage("The postal code must be exactly 5 digits.");
            })
            .Case("store_pickup", rules =>
            {
                rules.RuleFor(x => x.StoreCode)
                    .NotEmpty()
                    .Matches(@"^STR-[A-Z0-9]{4}$")
                        .WithMessage("The store code must have the format STR-XXXX.");
                rules.RuleFor(x => x.PickupWindow)
                    .NotNull()
                        .WithMessage("A pickup time window is required for in-store collection.")
                    .FutureDate()
                        .WithMessage("The pickup window must be in the future.")
                    .When(x => x.PickupWindow.HasValue);
            })
            .Case("locker", rules =>
            {
                rules.RuleFor(x => x.LockerId)
                    .NotEmpty()
                    .Matches(@"^LKR-\d{6}$")
                        .WithMessage("The locker ID must have the format LKR-XXXXXX.");
                rules.RuleFor(x => x.LockerAccessCode)
                    .NotEmpty()
                    .IsNumeric()
                    .LengthBetween(4, 8)
                        .WithMessage("The access code must be 4–8 digits.");
            });
    }
}
```

### Scenario 2: Notification Entity with Channel-Dependent Fields

A notification record must carry the right address for its delivery channel. The `Channel` field determines whether an email address, a phone number, or a device token is required.

```csharp
public class NotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty; // "email" | "sms" | "push"

    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? DeviceToken { get; set; }
    public string? PushPlatform { get; set; } // "ios" | "android"
}

public class NotificationValidator : AbstractValidator<NotificationRequest>
{
    public NotificationValidator()
    {
        // Common rules — always validated
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Channel)
            .NotEmpty()
            .In(new[] { "email", "sms", "push" })
                .WithMessage("The notification channel must be email, sms or push.")
            .StopOnFirstFailure();

        // Channel-specific rules
        RuleSwitch(x => x.Channel)
            .Case("email", rules =>
            {
                rules.RuleFor(x => x.RecipientEmail)
                    .NotEmpty()
                        .WithMessage("An email address is required for email notifications.")
                    .Email()
                        .WithMessage("The recipient email address is not valid.");
            })
            .Case("sms", rules =>
            {
                rules.RuleFor(x => x.RecipientPhone)
                    .NotEmpty()
                        .WithMessage("A phone number is required for SMS notifications.")
                    .PhoneNumber()
                        .WithMessage("The recipient phone number is not valid.");

                // SMS bodies are limited by character count
                rules.RuleFor(x => x.Body)
                    .MaximumLength(160)
                        .WithMessage("SMS messages cannot exceed 160 characters.");
            })
            .Case("push", rules =>
            {
                rules.RuleFor(x => x.DeviceToken)
                    .NotEmpty()
                        .WithMessage("A device token is required for push notifications.");

                rules.RuleFor(x => x.PushPlatform)
                    .NotEmpty()
                        .WithMessage("The push platform must be specified.")
                    .In(new[] { "ios", "android" })
                        .WithMessage("The push platform must be ios or android.");
            });
    }
}
```

### Combining RuleSwitch and Global Rules

Rules declared before `RuleSwitch` always execute regardless of which case matches. This lets you express a clean separation: global invariants at the top, variant-specific invariants inside the switch.

```csharp
public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        // Global rules: always evaluated
        RuleFor(x => x.Amount).Positive();
        RuleFor(x => x.Currency).NotEmpty().CurrencyCode();

        // Variant rules: only the matching case is evaluated
        RuleSwitch(x => x.Method)
            .Case("credit_card", rules =>
            {
                rules.RuleFor(x => x.CardNumber).NotEmpty().CreditCard();
                rules.RuleFor(x => x.Cvv).NotEmpty().MinimumLength(3).MaximumLength(4);
                rules.RuleFor(x => x.CardHolder).NotEmpty();
            })
            .Case("bank_transfer", rules =>
            {
                rules.RuleFor(x => x.Iban).NotEmpty().Iban();
                rules.RuleFor(x => x.BankName).NotEmpty();
            })
            .Case("paypal", rules =>
            {
                rules.RuleFor(x => x.PaypalEmail).NotEmpty().Email();
            });
    }
}
```

The `Amount` and `Currency` rules run first for every payment, then exactly one case from the switch runs based on `Method`. If `Method` does not match any case and no `.Default(...)` is provided, the switch contributes no additional errors.

---

## Next Steps

Once you have mastered these advanced patterns, you can explore:

- **[CascadeMode](08-cascade-mode.md)** — Fine-grained control of validation flow in complex cases
- **[Advanced Rules](06-advanced-rules.md)** — Custom and Transform for special cases
- **[Vali-Mediator Integration](14-valimediator-integration.md)** — Pipeline behavior with Result\<T\>
