using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;


namespace growTests
{
    public class FakeEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> SentEmails { get; }
            = new List<(string To, string Subject, string Body)>();

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            SentEmails.Add((email, subject, htmlMessage));
            return Task.CompletedTask;
        }
    }

}
