using System.Security.Cryptography;

namespace BasisBank.Identity.Api.Helpers {
    public class OtpGenerator {
        public static string GenerateRandomDigits(int length = 6) {
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
            }

            // ვიღებთ აბსოლუტურ მნიშვნელობას და ვაქცევთ 6 ციფრად
            int number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % (int)Math.Pow(10, length);
            return number.ToString("D" + length);
        }
    }
}
