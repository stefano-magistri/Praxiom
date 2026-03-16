using System.ComponentModel.DataAnnotations;

namespace DottoressaIorio.Models.Validation;

/// <summary>
/// Validates email addresses while still allowing null, empty, and whitespace-only values.
/// </summary>
public sealed class OptionalEmailAddressAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    /// <summary>
    /// Returns <see langword="true" /> for blank values and otherwise delegates to the built-in email validator.
    /// </summary>
    public override bool IsValid(object? value)
    {
        if (value is null) return true;

        if (value is string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;

            return EmailValidator.IsValid(email.Trim());
        }

        return false;
    }
}
