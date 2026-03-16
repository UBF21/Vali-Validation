using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Vali_Validation.Benchmarks.Models;
using Vali_Validation.Benchmarks.Validators;

namespace Vali_Validation.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ValidationBenchmarks
{
    private BenchmarkDtoValidator _validator = null!;
    private BenchmarkDto _validDto = null!;
    private BenchmarkDto _invalidDto = null!;

    [GlobalSetup]
    public void Setup()
    {
        _validator = new BenchmarkDtoValidator();
        _validDto = new BenchmarkDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };
        _invalidDto = new BenchmarkDto
        {
            Name = "",
            Email = "not-an-email",
            Age = -1
        };
    }

    [Benchmark(Description = "Validate valid object")]
    public bool ValidateValid()
    {
        return _validator.Validate(_validDto).IsValid;
    }

    [Benchmark(Description = "Validate invalid object")]
    public int ValidateInvalid()
    {
        return _validator.Validate(_invalidDto).ErrorCount;
    }

    [Benchmark(Description = "ValidateAsync valid object")]
    public async Task<bool> ValidateAsyncValid()
    {
        var result = await _validator.ValidateAsync(_validDto);
        return result.IsValid;
    }

    [Benchmark(Description = "ValidateAsync invalid object")]
    public async Task<int> ValidateAsyncInvalid()
    {
        var result = await _validator.ValidateAsync(_invalidDto);
        return result.ErrorCount;
    }

    [Benchmark(Baseline = true, Description = "Create validator instance")]
    public BenchmarkDtoValidator CreateValidator()
    {
        return new BenchmarkDtoValidator();
    }
}
