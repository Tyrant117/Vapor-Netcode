using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VaporMMO.SQLiteBackend
{
    public class SQLiteUtility
    {

        public const string passwordSalt = "sql_at_least_16_bytes";

        // PBKDF2 hashing recommended by NIST:
        // http://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-132.pdf
        // salt should be at least 128 bits = 16 bytes
        public static string PBKDF2Hash(string text, string salt)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(passwordSalt + salt);
            Rfc2898DeriveBytes pbkdf2 = new(text, saltBytes, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                static string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
