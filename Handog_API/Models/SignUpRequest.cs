namespace Handog_API.Models
{
    public class SignUpRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Locale { get; set; }
    }
}
