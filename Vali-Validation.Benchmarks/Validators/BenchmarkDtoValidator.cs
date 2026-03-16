using Vali_Validation.Benchmarks.Models;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Benchmarks.Validators;

public class BenchmarkDtoValidator : AbstractValidator<BenchmarkDto>
{
    public BenchmarkDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().Email();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(150);
    }
}
