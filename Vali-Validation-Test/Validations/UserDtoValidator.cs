using Vali_Validation_Test.Models;
using Vali_Validation.Core.Validators;

namespace Vali_Validation_Test.Validations;

public class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .NotNull()
            .DependentRuleAsync(
                x => x.Email,
                x => x.Name,
                async (email, name) =>
                {
                    await Task.Delay(50);
                    return email.StartsWith(name);
                }
            )
            .MinimumLength(3);

        RuleFor(x => x.Email)
            .NotEmpty()
            .NotNull()
            .Email();

        RuleFor(x => x.Age)
            .NotZero()
            .Positive();
    }
}