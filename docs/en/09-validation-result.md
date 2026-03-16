# Validation Result

`ValidationResult` is the object returned by `Validate()` and `ValidateAsync()`. It contains all errors found during validation, grouped by property name.

---

## Structure

```csharp
public class ValidationResult
{
    // Errors by property: { "Email": ["Invalid", "Already exists"] }
    public Dictionary<string, List<string>> Errors { get; }

    // Error codes by property: { "Email": ["INVALID_FORMAT", "ALREADY_EXISTS"] }
    public Dictionary<string, List<string>> ErrorCodes { get; }

    // true if there are no errors
    public bool IsValid { get; }

    // Total number of error messages (sum of all errors across all properties)
    public int ErrorCount { get; }

    // Names of properties that have errors
    public IReadOnlyList<string> PropertyNames { get; }
}
```

---

## IsValid and ErrorCount

```csharp
var result = await validator.ValidateAsync(request);

if (!result.IsValid)
{
    Console.WriteLine($"Validation failed with {result.ErrorCount} error(s).");
}

// IsValid is equivalent to: result.Errors.Count == 0
// ErrorCount is the total sum: if Email has 2 errors and Name has 1, ErrorCount is 3
```

---

## Accessing Errors

### By Property Directly

```csharp
if (result.Errors.TryGetValue("Email", out var emailErrors))
{
    foreach (var error in emailErrors)
        Console.WriteLine($"Email: {error}");
}
```

### Iterating All Errors

```csharp
foreach (var (propertyName, errors) in result.Errors)
{
    foreach (var error in errors)
    {
        Console.WriteLine($"{propertyName}: {error}");
    }
}
```

### Checking if a Property Has Errors

```csharp
bool hasEmailError = result.HasErrorFor("Email");
bool hasNameError = result.HasErrorFor("Name");
```

### Getting Errors for a Property

```csharp
// Returns an empty List<string> if there are no errors for that property
List<string> emailErrors = result.ErrorsFor("Email");

if (emailErrors.Count > 0)
{
    // ...
}
```

### Getting the First Error for a Property

```csharp
// Returns null if there are no errors for that property
string? firstError = result.FirstError("Email");

if (firstError != null)
{
    Console.WriteLine($"First Email error: {firstError}");
}
```

### PropertyNames

```csharp
// List of properties that have at least one error
IReadOnlyList<string> failedProperties = result.PropertyNames;

Console.WriteLine($"Properties with errors: {string.Join(", ", failedProperties)}");
// Output: "Properties with errors: Email, Password, BirthDate"
```

---

## AddError

`AddError` allows manually adding errors to a `ValidationResult`. Useful for combining validation with business logic:

```csharp
// Without error code
result.AddError("Email", "The email is already in use.");

// With error code
result.AddError("Email", "The email is already in use.", "EMAIL_ALREADY_EXISTS");
```

Usage example in a service that combines validation and logic:

```csharp
public async Task<ValidationResult> ValidateAndCheckBusinessRulesAsync(
    CreateOrderRequest request,
    CancellationToken ct)
{
    // First validate with the standard validator
    var result = await _validator.ValidateAsync(request, ct);

    // If there are already errors, do not continue with business rules
    if (!result.IsValid)
        return result;

    // Business rules that do not fit in the validator
    var customer = await _customers.GetByIdAsync(request.CustomerId, ct);
    if (customer.IsBlocked)
    {
        result.AddError("CustomerId",
            "The customer is blocked and cannot place orders.",
            "CUSTOMER_BLOCKED");
    }

    if (customer.CreditLimit < request.TotalAmount)
    {
        result.AddError("TotalAmount",
            $"The amount exceeds the customer's credit limit ({customer.CreditLimit:C}).",
            "CREDIT_LIMIT_EXCEEDED");
    }

    return result;
}
```

---

## ToFlatList

`ToFlatList()` returns all errors as a flat list of strings in the format `"PropertyName: message"`.

```csharp
var result = await validator.ValidateAsync(request);

// All errors in readable format
List<string> flatErrors = result.ToFlatList();
foreach (var error in flatErrors)
    Console.WriteLine(error);

// Output:
// Name: The name is required.
// Email: The email does not have a valid format.
// Email: The email is already registered.
// Password: Must have at least 8 characters.
```

Useful for logging:

```csharp
if (!result.IsValid)
{
    _logger.LogWarning("Validation failed: {Errors}",
        string.Join(" | ", result.ToFlatList()));
}
```

---

## Merge

`Merge` combines the errors from another `ValidationResult` into the current one. Errors are accumulated per property:

```csharp
var mainResult = await _mainValidator.ValidateAsync(request);
var addressResult = await _addressValidator.ValidateAsync(request.Address);
var paymentResult = await _paymentValidator.ValidateAsync(request.Payment);

// Combine all into one
mainResult.Merge(addressResult);
mainResult.Merge(paymentResult);

if (!mainResult.IsValid)
{
    // mainResult.Errors contains errors from all three validators
    return BadRequest(mainResult.Errors);
}
```

Example of merge with custom prefixes:

```csharp
public async Task<ValidationResult> ValidateComplexOrderAsync(
    ComplexOrderRequest request,
    CancellationToken ct)
{
    var result = new ValidationResult();

    // Validates the order header
    var headerResult = await _headerValidator.ValidateAsync(request, ct);
    result.Merge(headerResult);

    // Validates each line manually and adds errors with prefix
    for (int i = 0; i < request.Lines.Count; i++)
    {
        var lineResult = await _lineValidator.ValidateAsync(request.Lines[i], ct);
        foreach (var (property, errors) in lineResult.Errors)
        {
            foreach (var error in errors)
                result.AddError($"Lines[{i}].{property}", error);
        }
    }

    return result;
}
```

---

## ErrorCodes

`ErrorCodes` is a dictionary parallel to `Errors` that contains error codes assigned with `WithErrorCode`. A message may or may not have a code, independently.

```csharp
var result = await validator.ValidateAsync(request);

// Errors with their codes
foreach (var (property, codes) in result.ErrorCodes)
{
    Console.WriteLine($"{property}: {string.Join(", ", codes)}");
}

// Check if a specific code exists
bool hasCreditLimitError = result.ErrorCodes
    .Any(kvp => kvp.Value.Contains("CREDIT_LIMIT_EXCEEDED"));
```

### Usage in a Structured API Response

```csharp
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder(
    [FromBody] CreateOrderRequest request,
    [FromServices] IValidator<CreateOrderRequest> validator)
{
    var result = await validator.ValidateAsync(request, HttpContext.RequestAborted);

    if (!result.IsValid)
    {
        return BadRequest(new ApiErrorResponse
        {
            Message = "Validation has failed.",
            Errors = result.Errors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray()),
            ErrorCodes = result.ErrorCodes.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray())
        });
    }

    var order = await _orderService.CreateAsync(request);
    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; set; } = new();
    public Dictionary<string, string[]> ErrorCodes { get; set; } = new();
}
```

Response:

```json
{
  "message": "Validation has failed.",
  "errors": {
    "Email": ["The email is already registered."],
    "Amount": ["The amount exceeds the credit limit."]
  },
  "errorCodes": {
    "Email": ["EMAIL_ALREADY_EXISTS"],
    "Amount": ["CREDIT_LIMIT_EXCEEDED"]
  }
}
```

---

## Usage in ASP.NET Core with ValidationProblem

For Minimal API, `Results.ValidationProblem` expects `Dictionary<string, string[]>`:

```csharp
app.MapPost("/users", async (CreateUserRequest request, IValidator<CreateUserRequest> validator) =>
{
    var result = await validator.ValidateAsync(request);
    if (!result.IsValid)
    {
        var errors = result.Errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());

        return Results.ValidationProblem(errors);
    }

    // Process...
    return Results.Ok();
});
```

Automatic response in RFC 7807 format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["The email is already registered."]
  }
}
```

---

## Serialization

`ValidationResult` can be serialized directly with `System.Text.Json`:

```csharp
var result = await validator.ValidateAsync(request);
var json = JsonSerializer.Serialize(new
{
    isValid = result.IsValid,
    errors = result.Errors,
    errorCodes = result.ErrorCodes
});
```

---

## Building ValidationResult Manually

You can create a `ValidationResult` from scratch, useful in tests or in validation orchestrators:

```csharp
var result = new ValidationResult();
result.AddError("Name", "The name is required.", "NAME_REQUIRED");
result.AddError("Email", "The email is not valid.", "EMAIL_INVALID");
result.AddError("Email", "The email already exists.", "EMAIL_EXISTS");

Console.WriteLine(result.IsValid);      // false
Console.WriteLine(result.ErrorCount);   // 3
Console.WriteLine(result.PropertyNames.Count); // 2 (Name, Email)

foreach (var line in result.ToFlatList())
    Console.WriteLine(line);
// Name: The name is required.
// Email: The email is not valid.
// Email: The email already exists.
```

---

## Testing with ValidationResult

```csharp
public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator;

    public CreateProductValidatorTests()
    {
        _validator = new CreateProductValidator();
    }

    [Fact]
    public async Task Name_TooShort_ShouldFailWithCorrectMessage()
    {
        var request = new CreateProductRequest { Name = "AB", Price = 10m };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
        Assert.Contains("at least 3 characters", result.FirstError("Name"));
    }

    [Fact]
    public async Task ValidRequest_ShouldPass()
    {
        var request = new CreateProductRequest
        {
            Name = "Laptop Pro",
            Price = 999.99m,
            Stock = 10,
            Category = "Electronics"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task MultipleErrors_AllReported()
    {
        var request = new CreateProductRequest { Name = "", Price = -5m };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
        Assert.True(result.HasErrorFor("Price"));

        // Ensure all errors are present
        var flatErrors = result.ToFlatList();
        Assert.Contains(flatErrors, e => e.Contains("Name"));
        Assert.Contains(flatErrors, e => e.Contains("Price"));
    }

    [Fact]
    public async Task WithErrorCode_ReturnsCorrectCode()
    {
        var request = new CreateProductRequest { Name = "", Price = 10m };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.ErrorCodes.ContainsKey("Name"));
        Assert.Contains("NAME_REQUIRED", result.ErrorCodes["Name"]);
    }
}
```

---

## Next Steps

- **[Exceptions](10-exceptions.md)** — ValidationException and ValidateAndThrow
- **[ASP.NET Core](12-aspnetcore-integration.md)** — Integration with middleware and filters that use ValidationResult
