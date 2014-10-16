using uComponents.DataTypes.MultiUrlPicker.Dto;
using uComponents.DataTypes.UrlPicker;
using uComponents.DataTypes.UrlPicker.Dto;
using Umbraco;
using Umbraco.Core.Models;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web;
using umbraco;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Core.Logging;
using umbraco.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;

namespace PG.UmbracoExtensions.Helpers
{
    /// <summary>
    /// Helpers to get/convert/etc. data from umbraco node/field should go here
    /// </summary>
    public static class UmbracoNodeHelpers
    {
        static HttpServerUtility Server = HttpContext.Current.Server;

        /// <summary>
        /// Generates css class for current node from its doctype and name
        /// </summary>
        /// <param name="currentNode"></param>
        /// <returns>
        /// Doctypes: post, category, homepage, other-type
        /// </returns>
        public static String GenerateCSSClass(IPublishedContent currentNode)
        {
            //TODO: solve multilanguage issue (url names will differ for same pages)
            String result = "other-type";

            if (currentNode != null)
            {
                var typeService = ApplicationContext.Current.Services.ContentTypeService;
                IContentType nodeType = typeService.GetContentType(currentNode.DocumentTypeAlias);
                if (nodeType != null)
                {
                    if (
                        nodeType.ContentTypeCompositionExists("Category") ||
                        currentNode.DocumentTypeAlias == "Category" ||
                        currentNode.DocumentTypeAlias == "SearchResults" ||
                        currentNode.DocumentTypeAlias == "List"
                        )
                    { 
                        result = "category";
                    }
                    else if (
                        nodeType.ContentTypeCompositionExists("Homepage") ||
                        currentNode.DocumentTypeAlias == "Homepage"
                        )
                    {
                        result = "homepage";
                    }
                    else if (
                        nodeType.ContentTypeCompositionExists("SimplePage") ||
                        currentNode.DocumentTypeAlias == "Contact" ||
                        currentNode.DocumentTypeAlias == "ListElement" ||
                        currentNode.DocumentTypeAlias == "SimplePage" ||
                        currentNode.DocumentTypeAlias == "Site_Infographic"
                        )
                    {
                        result = "post";
                    }
                }
                IEnumerable<IPublishedContent> ancestors = currentNode.AncestorsOrSelf();
                foreach (IPublishedContent ancestor in ancestors)
                {
                    if (ancestor.Level > 0)
                    {
                        result += " con_" + ancestor.UrlName;
                    }
                }

                //add document type
                var projectPrefixes = new string[] {"Site_", "Academy_", "Creative_" };
                var doctypeName = currentNode.DocumentTypeAlias;
                foreach (var prefix in projectPrefixes)
                {
                    doctypeName = doctypeName.Replace(prefix, "");
                }
                doctypeName = Regex.Replace(doctypeName, "(\\B[A-Z])", "-$1");
                doctypeName = doctypeName.ToCharArray()[0] == '-' ? doctypeName.Substring(1) : doctypeName ;
                result += " doc_" + doctypeName.ToLower().Replace(" ", "-").Replace("_", "");
            }
            return result;
        }

        /// <summary>
        /// Gets the url picker url
        /// </summary>
        /// <param name="urlPickerValue"></param>
        /// <returns></returns>
        public static String GetPickerUrl(String urlPickerValue)
        {
            String result = "";

            if (!String.IsNullOrEmpty(urlPickerValue))
            {
                
                //for optimization: check if node id is selected, get node and get direct url (Deserialize() gets node and puts only url to result)
                var parameters = urlPickerValue.Split(',');
                if (parameters.Length > 0)
                {
                    var mode = (UrlPickerMode)Enum.Parse(typeof(UrlPickerMode), parameters[0], false);
                    if (mode == UrlPickerMode.Content && parameters.Length > 2)
                    {
                        int nodeId;
                        if (Int32.TryParse(parameters[2], out nodeId))
                        {
                            var node = UmbracoContext.Current.ContentCache.GetById(nodeId);
                            result = node.GetDirectUrl();
                        }
                    }
                    else
                    {
                        result = UrlPickerState.Deserialize(urlPickerValue).Url; 
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets content node from url picker
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fieldAlias"></param>
        /// <returns></returns>
        public static IPublishedContent GetPickerNode(this IPublishedContent node, string fieldAlias = "urlDestination")
        {
            IPublishedContent result = null;
            if (node != null && node.HasValue(fieldAlias))
            {
                var pickerValue = node.GetPropertyValue<string>(fieldAlias);
                var nodeId = UrlPickerState.Deserialize(pickerValue).NodeId;
                if (nodeId != null && nodeId > 0)
                {
                    result = UmbracoContext.Current.ContentCache.GetById((int) nodeId);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets url picker url
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fieldAlias"></param>
        /// <returns></returns>
        public static string GetPickerUrl(this IPublishedContent node, string fieldAlias)
        {
            string result = "";

            if (node.HasProperty(fieldAlias))
            {
                result = GetPickerUrl(node.GetPropertyValue<string>(fieldAlias));
            }

            return result;
        }

        /// <summary>
        /// Gets url picker url and checks the url
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fieldAlias"></param>
        /// <returns></returns>
        public static string GetPickerAbsoluteUrl(this IPublishedContent node, string fieldAlias)
        {
            string result = "";

            if (node.HasProperty(fieldAlias))
            {
                result = GetPickerUrl(node.GetPropertyValue<string>(fieldAlias));
                result = GlobalHelpers.GetAbsoluteUrl(result);
            }
            return result;
        }


        /// <summary>
        /// Gets the url picker url
        /// NOTE: gets adds only links with title
        /// </summary>
        /// <param name="urlPickerValue"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetMultiPickerUrl(String urlPickerValue)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(urlPickerValue))
            {

                foreach (var value in MultiUrlPickerState.Deserialize(urlPickerValue).Items)
                {
                    if (!String.IsNullOrEmpty(value.Title) && !String.IsNullOrEmpty(value.Url))
                    {
                        result.Add(value.Title, value.Url);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the url picker url
        /// </summary>
        /// <param name="urlPickerValue"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetMultiPickerUrl(this IPublishedContent node, string fieldAlias)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (node.HasProperty(fieldAlias))
            {
                result = GetMultiPickerUrl(node.GetPropertyValue<string>(fieldAlias));
            }

            return result;
        }

        /// <summary>
        /// Gets link target value from node.
        /// 
        /// Returns "_self" if target is not specified
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static String GetTargetValue(this IPublishedContent node, string fieldAlias = "linkTarget")
        {
            String result = "_self";

            if (node.HasValue(fieldAlias))
            {
                result = library.GetPreValueAsString(Convert.ToInt32(node.GetPropertyValue(fieldAlias)));
            }

            return result;
        }

        /// <summary>
        /// Gets selected users list from Multi User Select type field
        /// </summary>
        /// <param name="node"></param>
        /// <param name="propertyAlias"></param>
        /// <returns></returns>
        public static IEnumerable<User> GetSelectedUsers(IPublishedContent node, String propertyAlias)
        {
            List<User> selectedUsers = new List<User>();

            if (node != null && !String.IsNullOrEmpty(propertyAlias) && node.HasProperty(propertyAlias) && node.HasValue(propertyAlias))
            {
                var userIds = node.GetPropertyValue<String>(propertyAlias).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var selectedUserId in userIds)
                {
                    User user = User.GetUser(Int32.Parse(selectedUserId));
                    if (user != null)
                    {
                        selectedUsers.Add(user);
                    }
                }
            }

            return selectedUsers;
        }

        /// <summary>
        /// Checks if passed node is category (from adform umbraco bootstrap doctype structure)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsCategory(IPublishedContent node)
        {
            bool result = false;

            var doctype = ApplicationContext.Current.Services.ContentTypeService.GetContentType(node.DocumentTypeId);
            if (doctype.ContentTypeCompositionExists("Category") || doctype.ContentTypeCompositionExists("AbstractCategory") || doctype.Alias == "Category" || doctype.Alias == "AbstractCategory")
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Returns relative to root element url
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string RelativeUrl(this IPublishedContent node)
        {
            var rootNode = node.AncestorOrSelf();
            var rootReplaceUrl = "/";
            
            //check if root node is included in url
            var rootUrl = rootNode.Url;
            var pattern = String.Format("/{0}/$", rootNode.UrlName);
            if (Regex.IsMatch(rootUrl, pattern))
            {
                rootReplaceUrl = "/" + rootNode.UrlName + "/";
            }

            return node.Url.Replace(rootNode.Url, rootReplaceUrl);
        }

        /// <summary>
        /// Returns direct url which can be different based on doctype (ex.: creative space preview, case study download link)
        /// 
        /// TODO: create better, more flexible solution to doctype mapping: label on doctype or extend PropertyTypeMap
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetDirectUrl(this IPublishedContent node)
        {
            var result = node.Url;

            switch (node.DocumentTypeAlias)
            {
                case "Site_Collateral" :
                case "Site_CaseStudy" :
                    if (node.HasProperty("hideDownloadForm") && node.GetPropertyValue<bool>("hideDownloadForm") != true)
                    {
                        result = node.GetDownloadUrl();
                    }
                    else
                    {
                        result = node.GetPickerUrl("fileUrl");
                    }
                    break;
                case "Site_CreativeSpacePost" :
                    result = node.GetPickerUrl("fileUrl");
                    break;
                case "Site_RichMediaFormat" :
                    result = node.GetPickerUrl("desktopPreview");
                    break;
            }

            return result;
        }

        /// <summary>
        /// Gets integer id list from content field. For use with multi-node picker fields.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fieldAlias"></param>
        /// <returns></returns>
        public static List<int> GetIdList(this IPublishedContent node, string fieldAlias)
        {
            List<int> result = new List<int>();

            if (node.HasValue(fieldAlias))
            {
                var valueStr = node.GetProperty(fieldAlias).Value.ToString();
                result.AddRange(valueStr.ToIntList());
            }

            return result;
        }

        /// <summary>
        /// Simple helper for getting first Multi-Node Tree Picker property node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fieldAlias"></param>
        /// <returns></returns>
        public static IPublishedContent GetTreePickerNode(this IPublishedContent node, string fieldAlias)
        {
            return node.GetPropertyValue<IEnumerable<IPublishedContent>>(fieldAlias).FirstOrDefault();
        }
    }
}
