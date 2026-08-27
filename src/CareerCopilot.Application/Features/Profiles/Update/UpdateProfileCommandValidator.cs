using CareerCopilot.Application.Features.Profiles.Dtos;
using FluentValidation;

namespace CareerCopilot.Application.Features.Profiles.Update;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200);

        RuleFor(x => x.Headline)
            .MaximumLength(300);

        RuleFor(x => x.TargetRole)
            .MaximumLength(200);

        RuleFor(x => x.TargetIndustries)
            .MaximumLength(500);

        RuleFor(x => x.ProfessionalSummary)
            .MaximumLength(5000);

        RuleFor(x => x.CareerGoals)
            .MaximumLength(2000);

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(60);

        RuleFor(x => x.Skills)
            .Must(skills => skills is null || skills.Count <= 100)
            .WithMessage("A profile can reference at most 100 skill entries.");
    }
}