namespace backend_asp.Models.DTO
{
    public class AuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public List<string> Message { get; set; }
        public bool Success { get; set; }

    }
}