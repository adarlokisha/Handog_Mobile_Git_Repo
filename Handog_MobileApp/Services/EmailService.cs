using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace Handog_MobileApp.Services
{
    internal class EmailService
    {
        // ⚠️ Store your API key securely in environment variables or secrets manager
        private readonly string apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");

        public async Task SendVerificationEmail(string userEmail, string code)
        {
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("no-reply@handog.work", "Handog App");
            var subject = "Verify Your Signup";
            var to = new EmailAddress(userEmail);
            var plainTextContent = $"Your verification code is {code}";
            var htmlContent = $"<strong>Your verification code is {code}</strong>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            Console.WriteLine(response.StatusCode == System.Net.HttpStatusCode.Accepted
                ? "Verification email sent successfully!"
                : $"Error sending verification email: {response.StatusCode}");
        }

        public async Task SendPasswordResetEmail(string userEmail, string resetLink)
        {
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("no-reply@handog.work", "Handog App");
            var subject = "Password Reset Request";
            var to = new EmailAddress(userEmail);
            var plainTextContent = $"Click here to reset your password: {resetLink}";
            var htmlContent = $"<a href='{resetLink}'>Reset your password</a>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            Console.WriteLine(response.StatusCode == System.Net.HttpStatusCode.Accepted
                ? "Password reset email sent successfully!"
                : $"Error sending password reset email: {response.StatusCode}");
        }
    }
}
