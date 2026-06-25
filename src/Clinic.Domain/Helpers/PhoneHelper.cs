using System;
using System.Linq;

namespace Clinic.Domain.Helpers;

public static class PhoneHelper
{
    private static readonly string[] CountryCodes = new[] 
    { 
        "+20", "+966", "+971", "+380", "+359", "+249", "+212", 
        "+213", "+216", "+218", "+44", "+49", "+33", "+91", "+86", "+1" 
    };

    public static (string CountryCode, string PhoneNumber) SplitContactNumber(string? contactNumber)
    {
        if (string.IsNullOrWhiteSpace(contactNumber))
        {
            return ("+20", "");
        }

        contactNumber = contactNumber.Trim();

        // If there is a space separating country code and phone number, split it!
        var parts = contactNumber.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && parts[0].StartsWith("+") && double.TryParse(parts[0].Substring(1), out _))
        {
            return (parts[0], string.Join("", parts.Skip(1)));
        }

        // Check if it starts with "+"
        if (contactNumber.StartsWith("+"))
        {
            foreach (var prefix in CountryCodes)
            {
                if (contactNumber.StartsWith(prefix))
                {
                    var number = contactNumber.Substring(prefix.Length).Trim();
                    return (prefix, number);
                }
            }

            // Fallback: take first 4 characters (+ plus 3 digits) or similar
            if (contactNumber.Length >= 4 && char.IsDigit(contactNumber[1]) && char.IsDigit(contactNumber[2]) && char.IsDigit(contactNumber[3]))
            {
                return (contactNumber.Substring(0, 4), contactNumber.Substring(4));
            }
            if (contactNumber.Length >= 3 && char.IsDigit(contactNumber[1]) && char.IsDigit(contactNumber[2]))
            {
                return (contactNumber.Substring(0, 3), contactNumber.Substring(3));
            }
            if (contactNumber.Length >= 2 && char.IsDigit(contactNumber[1]))
            {
                return (contactNumber.Substring(0, 2), contactNumber.Substring(2));
            }
        }

        // If it doesn't start with "+", default country code is "+20" (Egypt)
        // But if it starts with "0020", it is equivalent to "+20"
        if (contactNumber.StartsWith("0020"))
        {
            return ("+20", contactNumber.Substring(4));
        }

        return ("+20", contactNumber);
    }

    public static (bool IsValid, string ErrorMessage) ValidatePhoneNumber(string? countryCode, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return (false, "Country code is required.");
        }
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return (false, "Phone number is required.");
        }

        var cleanPhone = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();

        if (!cleanPhone.All(char.IsDigit))
        {
            return (false, "Phone number must contain digits only.");
        }

        if (countryCode == "+20")
        {
            // Strip leading zero if present
            if (cleanPhone.StartsWith("0"))
            {
                cleanPhone = cleanPhone.Substring(1);
            }

            // Check if it starts with a mobile prefix
            var hasMobilePrefix = cleanPhone.StartsWith("10") || cleanPhone.StartsWith("11") || cleanPhone.StartsWith("12") || cleanPhone.StartsWith("15");

            if (hasMobilePrefix)
            {
                // Egyptian mobile must be exactly 10 digits (e.g. 1012345678)
                if (cleanPhone.Length != 10)
                {
                    return (false, "Invalid Egyptian mobile number. Must be 11 digits starting with 010, 011, 012, or 015.");
                }
            }
            else
            {
                // Landlines are 7-9 digits
                if (cleanPhone.Length < 7 || cleanPhone.Length > 9)
                {
                    return (false, "Invalid Egyptian landline number. Must be 7-9 digits.");
                }
            }
        }
        else
        {
            // General validation: between 6 and 15 digits
            if (cleanPhone.Length < 6 || cleanPhone.Length > 15)
            {
                return (false, "Phone number must be between 6 and 15 digits.");
            }
        }

        return (true, string.Empty);
    }

    public static string NormalizePhoneNumber(string? countryCode, string phoneNumber)
    {
        var clean = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();
        if (countryCode == "+20" && clean.StartsWith("0"))
        {
            clean = clean.Substring(1);
        }
        return clean;
    }
}
