using Serilog;
using System.Net;
using System.Net.Mail;

namespace DayCare_ManagementSystem_API.Service
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string schoolName;
        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
            schoolName = Environment.GetEnvironmentVariable("SchoolName")!;
        }

        #region [ Send Template Email ]

        public async Task<string> SendTemplateEmail(List<string> recipents, string subject, string htmlbody)
        {
            try
            {
                string smtpServer = Environment.GetEnvironmentVariable("SMTPServer");
                int smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTPServerPort"));
                string smtpUsernameEmail = Environment.GetEnvironmentVariable("EmailSender");
                string smtpPassword = Environment.GetEnvironmentVariable("EmailPassword");

                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(smtpUsernameEmail, smtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpUsernameEmail),
                    Subject = subject,
                    Body = htmlbody.Replace("{{School Name}}", schoolName),
                    IsBodyHtml = true
                };

                foreach (string emailRecipient in recipents)
                {
                    mailMessage.To.Add(emailRecipient);
                }

                await smtpClient.SendMailAsync(mailMessage);

                return "Sent";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in while trying to send an email");
                throw;
            }
        }

        #endregion

        //public string PrepareEmailTemplates(string path, string templateName)
        //{
        //    try
        //    {
        //        if (templateName.ToLower() == "applicationreceived.html" )
        //        {
        //            var template = System.IO.File.ReadAllText(path).Replace("\n", "");
        //            template = template.Replace("{{FailedPlaceholder}}", failed.ToString())
        //                                .Replace("{{PassedPlaceholder}}", passed.ToString())
        //                                .Replace("{{MetadataPlaceholder}}", noRecordingForMetadata.ToString())
        //                                .Replace("{{RetrievedPlaceholder}}", metaDataCount.ToString());

        //            return template;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in the EmailService in the GetTemplate method.");
        //        throw;
        //    }
        //}
    }
}
