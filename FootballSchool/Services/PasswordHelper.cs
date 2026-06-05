using System;
using System.Security.Cryptography;

namespace FootballSchool.Services
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;
        private const string Prefix = "PBKDF2";

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string storedPassword, string enteredPassword)
        {
            if (string.IsNullOrEmpty(storedPassword) || string.IsNullOrEmpty(enteredPassword))
                return false;

            if (!IsHashedPassword(storedPassword))
                return storedPassword == enteredPassword;

            try
            {
                string[] parts = storedPassword.Split('$');
                if (parts.Length != 4 || parts[0] != Prefix)
                    return false;

                int iterations = int.Parse(parts[1]);
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] savedHash = Convert.FromBase64String(parts[3]);

                byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
                    enteredPassword,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    savedHash.Length);

                return CryptographicOperations.FixedTimeEquals(savedHash, enteredHash);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHashedPassword(string password)
        {
            return !string.IsNullOrEmpty(password) && password.StartsWith(Prefix + "$", StringComparison.Ordinal);
        }
    }
}
