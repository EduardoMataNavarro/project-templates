

namespace backend_asp.Configurations
{
    public class JwtConfig {
        public string Secret { get; set; } = String.Empty;
        public string TTL { get; set; }
    }
}