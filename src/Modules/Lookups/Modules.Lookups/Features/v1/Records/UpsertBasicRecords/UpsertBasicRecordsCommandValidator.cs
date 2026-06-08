using FluentValidation;
using FSH.Modules.Lookups.Contracts.v1.Records.UpsertBasicRecords;

namespace FSH.Modules.Lookups.Features.v1.Records.UpsertBasicRecords;

public sealed class UpsertBasicRecordsCommandValidator : AbstractValidator<UpsertBasicRecordsCommand>
{
    public UpsertBasicRecordsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleForEach(x => x.Records).ChildRules(r =>
        {
            r.RuleFor(i => i.Code)
                .NotEmpty().MaximumLength(64)
                .Matches(@"^[A-Za-z0-9_\-]+$")
                .WithMessage("El código del registro solo admite letras, números, guion y guion bajo.");
            r.RuleFor(i => i.Value).NotEmpty().MaximumLength(256);
        });
        RuleFor(x => x.Records)
            .Must(rs => rs.Select(i => i.Code.Trim().ToLowerInvariant()).Distinct().Count() == rs.Count)
            .WithMessage("Hay códigos de registro duplicados.");
    }
}
