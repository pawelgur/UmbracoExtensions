using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using umbraco.BusinessLogic;
using Umbraco.Core.Logging;
using System.IO;
using System.Text.RegularExpressions;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;

namespace PG.UmbracoExtensions.Helpers
{
    /// <summary>
    /// Simple class for sending emails in Umbraco
    /// </summary>
    public static class MailHelper
    {
        public static bool SendMailTemplate(String subject, String templateUrl, String from, IEnumerable<User> recipients, Dictionary<String, String> templateData)
        {
            String content = "";

            try
            {
                String filePath = HttpContext.Current.Server.MapPath(templateUrl);
                content = System.IO.File.ReadAllText(filePath);
                if (!String.IsNullOrEmpty(content))
                {
                    foreach (String parameter in templateData.Keys)
                    {
                        content = content.Replace("$$" + parameter + "$$", templateData[parameter]);
                    }
                    content = Regex.Replace(content, @"\$\$(.*?)\$\$", "");
                    return SendMail(subject, content, from, recipients);
                }
            }
            catch (FileNotFoundException ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, String.Format("Error creating email, template not found. Subject: {0}, Template url:{1} ", subject, templateUrl), ex);
            }

            return false;
        }

        public static bool SendMail(String subject, String content, String from, IEnumerable<User> recipients)
        {
            List<String> recipientsStrings = new List<string>();
            if (recipients != null)
            {
                foreach (var recipient in recipients)
                {
                    if (!String.IsNullOrEmpty(recipient.Email))
                    {
                        recipientsStrings.Add(recipient.Email);
                    }
                }
            }

            return SendMail(subject, content, from, recipientsStrings);
        }


        public static bool SendMail(String subject, String content, String from, IEnumerable<String> recipients, String replyTo = null)
        {
            try
            {
                var mailMsg = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(from),
                    Subject = subject,
                    Body = content,
                    IsBodyHtml = true
                };

                foreach (var recipient in recipients)
                {
                    if (!String.IsNullOrEmpty(recipient))
                    {
                        mailMsg.To.Add(new System.Net.Mail.MailAddress(HttpUtility.HtmlEncode(recipient)));
                    }
                }

                if (!String.IsNullOrEmpty(replyTo))
                {
                    mailMsg.ReplyToList.Add(new System.Net.Mail.MailAddress(replyTo));
                }

                var smtpClient = new System.Net.Mail.SmtpClient();

                smtpClient.Send(mailMsg);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Error creating or sending email: " + subject, ex);
            }

            return false;
        }

        
        
        /// <summary>
        /// Gets email recipients from root node by fieldName
        /// NOTE: if current node from umbraco context is not available - gets first homepage with urlname "home"
        /// TODO: get current culture homepage
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static IEnumerable<umbraco.BusinessLogic.User> GetEmailRecipients(string fieldName)
        {
            IEnumerable<umbraco.BusinessLogic.User> result = new List<umbraco.BusinessLogic.User>();

            var UmbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            IPublishedContent rootNode = null;

            try
            {
                rootNode = UmbracoHelper.AssignedContentItem;
            }
            catch (InvalidOperationException exc)
            {
                //current node not available
                rootNode = UmbracoHelper.TypedContentAtRoot().FirstOrDefault(x => x.DocumentTypeAlias == "Site_Homepage");
            }
            
            result = rootNode.GetSelectedUsers(fieldName);

            return result;
        }
    }
}
