using CareerCopilot.Application.Features.Jobs.Dtos;
using FluentValidation;

namespace CareerCopilot.Application.Features.Jobs.Create;

public sealed class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(200);

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Paste the job description.")
            .MaximumLength(20000);
    }
}