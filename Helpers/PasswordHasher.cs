using System.Security.Cryptography;
using System.Text;

namespace JiASsist.Helpers
{
    public static class PasswordHasher
    {
        // Format: {iterations}.{saltBase64}.{hashBase64}
        public static string Hash(string password, int iterations = 100_000)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string hashedPassword, string providedPassword)
        {
            try
            {
                var parts = hashedPassword.Split('.');
                if (parts.Length != 3) return false;

                var iterations = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var hash = Convert.FromBase64String(parts[2]);

                using var pbkdf2 = new Rfc2898DeriveBytes(providedPassword, salt, iterations, HashAlgorithmName.SHA256);
                var computed = pbkdf2.GetBytes(hash.Length);

                return CryptographicOperations.FixedTimeEquals(computed, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
