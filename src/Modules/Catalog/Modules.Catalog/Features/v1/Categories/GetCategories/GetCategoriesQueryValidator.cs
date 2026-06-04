using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Categories.GetCategories;

namespace FSH.Modules.Catalog.Features.v1.Categories.GetCategories;

/// <summary>
/// GetCategoriesQuery returns a full tree (not paged).
/// Minimal validator to satisfy the Architecture.Tests requirement.
/// </summary>
public sealed class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        // No additional rules — the query returns all categories as a tree.
    }
}
