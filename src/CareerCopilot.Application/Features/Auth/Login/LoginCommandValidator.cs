using FluentValidation;

namespace CareerCopilot.Application.Features.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}