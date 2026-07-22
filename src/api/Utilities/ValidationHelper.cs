using System.Net.Mail;
using Template.Api.Exceptions;

namespace Template.Api.Utilities;

public static class ValidationHelper
{
    public static void ValidateRequiredString(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CustomException($"{fieldName} is required.");
        }
    }

    public static void ValidateEmail(string? email, string fieldName = "Email")
    {
        ValidateRequiredString(email, fieldName);

        try
        {
            _ = new MailAddress(email!.Trim());
        }
        catch
        {
            throw new CustomException("Invalid email format.");
        }
    }
}
