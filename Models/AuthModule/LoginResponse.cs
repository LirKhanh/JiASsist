namespace JiASsist.Models.AuthModule
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public User? User { get; set; }
    }
}
