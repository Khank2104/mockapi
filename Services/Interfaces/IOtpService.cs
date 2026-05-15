namespace UserManagementSystem.Services
{
    public interface IOtpService
    {
        string GenerateAndSaveOtp(string key, TimeSpan expiry);
        bool VerifyAndRemoveOtp(string key, string otpToVerify);
        void SaveDataToCache<T>(string key, T data, TimeSpan expiry);
        T? GetDataFromCache<T>(string key);
        void RemoveCache(string key);
    }
}
