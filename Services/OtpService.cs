using Microsoft.Extensions.Caching.Memory;

namespace UserManagementSystem.Services
{
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateAndSaveOtp(string key, TimeSpan expiry)
        {
            // Sử dụng RandomNumberGenerator để bảo mật hơn (Cryptographically Secure)
            byte[] bytes = new byte[4];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            int number = Math.Abs(BitConverter.ToInt32(bytes, 0) % 900000) + 100000;
            string otp = number.ToString();

            _cache.Set(key, otp, expiry);
            return otp;
        }

        public bool VerifyAndRemoveOtp(string key, string otpToVerify)
        {
            if (_cache.TryGetValue(key, out string? savedOtp) && savedOtp == otpToVerify)
            {
                _cache.Remove(key);
                return true;
            }
            return false;
        }

        public void SaveDataToCache<T>(string key, T data, TimeSpan expiry)
        {
            _cache.Set(key, data, expiry);
        }

        public T? GetDataFromCache<T>(string key)
        {
            _cache.TryGetValue(key, out T? data);
            return data;
        }

        public void RemoveCache(string key)
        {
            _cache.Remove(key);
        }
    }
}
