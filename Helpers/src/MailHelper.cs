using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Umbraco.Core.Logging;
using System.IO;
using System.Text.RegularExpressions;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;
using User = umbraco.BusinessLogic.User;

namespace PG.UmbracoExtensions.Helpers
{
    /// <summary>
    /// Simple class for sending emails in Umbraco
    /// </summary>
    public static class MailHelper
    {
        /// <summary>
        /// Send email using template
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="templateUrl">template url relative to web root</param>
        /// <param name="from"></param>
        /// <param name="recipients"></param>
        /// <param name="templateData"></param>
        /// <returns></returns>
        public static bool SendMailTemplate(String subject, String templateUrl, String from, IEnumerable<IUser> recipients, Dictionary<String, String> templateData)
        {
            var content = GetTemplateContent(templateUrl, templateData);
            if (!String.IsNullOrEmpty(content))
            {
                return SendMail(subject, content, from, recipients);
            }

            return false;
        }

        /// <summary>
        /// Send email using template
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="templateUrl">template url relative to web root</param>
        /// <param name="from"></param>
        /// <param name="recipients">comma seperated emails</param>
        /// <param name="templateData"></param>
        /// <returns></returns>
        public static bool SendMailTemplate(String subject, String templateUrl, String from, string recipients, Dictionary<String, String> templateData)
        {
            var content = GetTemplateContent(templateUrl, templateData);
            if (!String.IsNullOrEmpty(content))
            {
                return SendMail(subject, content, from, recipients);
            }

            return false;
        }


        public static bool SendMail(String subject, String content, String from, IEnumerable<IUser> recipients)
        {
            var recipientsStrings = GetEmailList(recipients);
            return SendMail(subject, content, from, recipientsStrings);
        }

        public static bool SendMail(String subject, String content, String from, string recipients)
        {
            var recipientsStrings = GetEmailList(recipients);
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
        /// Gets email recipients from root node by fieldName from Multi User Select field
        /// 
        /// NOTE: if current node from umbraco context is not available or context node not passed - gets first node with doctype containing "Homepage"
        /// TODO: get current culture homepage
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="contextNodeId">id of current page used to get current root node</param>
        /// <returns></returns>
        [Obsolete("Multi User Select shouldn't be used for email lists.")]
        public static IEnumerable<IUser> GetEmailRecipients(string fieldName, int contextNodeId = 0)
        {
            IEnumerable<IUser> result = new List<IUser>();

            var UmbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            IPublishedContent rootNode = contextNodeId == 0 
                ? null 
                : UmbracoContext.Current.ContentCache.GetById(contextNodeId);

            rootNode = rootNode == null ? null : rootNode.AncestorOrSelf(1);

            if (rootNode == null)
            {
                try
                {
                    rootNode = UmbracoHelper.AssignedContentItem.AncestorOrSelf(1);
                }
                catch (Exception exc)
                {
                    //current node not available
                    rootNode = UmbracoHelper.TypedContentAtRoot().FirstOrDefault(x => x.DocumentTypeAlias.Contains("Homepage"));
                }
            }

            result = rootNode.GetSelectedUsers(fieldName);

            return result;
        }




        #region HelperMethods

        static string GetTemplateContent(String templateUrl, Dictionary<String, String> templateData)
        {
            string content = "";
            
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
                }
            }
            catch (FileNotFoundException ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, String.Format("Error creating email, template not found. Template url:{0} ", templateUrl), ex);
            }

            return content;

        }

        static IEnumerable<string> GetEmailList(IEnumerable<IUser> users)
        {
            List<String> recipientsStrings = new List<string>();
            if (users != null)
            {
                foreach (var user in users)
                {
                    if (!String.IsNullOrEmpty(user.Email))
                    {
                        recipientsStrings.Add(user.Email);
                    }
                }
            }

            return recipientsStrings;
        }

        static IEnumerable<string> GetEmailList(string recipientsString)
        {
            List<String> emails = new List<string>();

            if (!String.IsNullOrEmpty(recipientsString))
            {
                var parts = recipientsString.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var email in parts)
                {
                    emails.Add(email.Trim());
                }
            }

            return emails;
        }

        #endregion
    }
}
