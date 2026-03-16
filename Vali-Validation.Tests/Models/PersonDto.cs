namespace Vali_Validation.Tests.Models;

public class AddressDto
{
    public string? Street { get; set; }
    public string? City { get; set; }
}

public class PersonDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public List<string>? Tags { get; set; }
    public AddressDto? Address { get; set; }
    public string? Status { get; set; }
    public string? GuidValue { get; set; }
}
