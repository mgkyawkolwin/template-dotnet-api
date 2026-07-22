using System.Net.Mail;
using Microsoft.Extensions.Localization;
using Template.Api.Exceptions;

namespace Template.Api.Utilities;

public static class ValidationHelper
{
    public static void ValidateRequiredGuid(IStringLocalizer localizer, string fieldName, Guid? value)
    {
        if (!value.HasValue || value == Guid.Empty)
        {
            throw new CustomException(localizer[$"Template.Required", fieldName]);
        }
    }

    public static void ValidateRequiredString(IStringLocalizer localizer, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CustomException(localizer[$"Template.Required", fieldName]);
        }
    }

    public static void ValidateEmail(IStringLocalizer localizer, string fieldName, string? email)
    {
        ValidateRequiredString(localizer, fieldName, email);

        try
        {
            _ = new MailAddress(email!.Trim());
        }
        catch
        {
            throw new CustomException(localizer[$"Template.InvalidFormat", fieldName]);
        }
    }
}
