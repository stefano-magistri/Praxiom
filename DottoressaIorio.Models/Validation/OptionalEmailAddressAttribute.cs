using System.ComponentModel.DataAnnotations;

namespace DottoressaIorio.Models.Validation;

public sealed class OptionalEmailAddressAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return true;
            }

            return EmailValidator.IsValid(email.Trim());
        }

        return false;
    }
}
