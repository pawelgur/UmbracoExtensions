using System.Web.Caching;
using System.Xml;
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
using System.Net.Http;  
using System.ServiceModel.Channels;

 
namespace PG.UmbracoExtensions.Helpers
{
    /// <summary>
    /// Global helpers central class (uncategorized methods)
    /// </summary>
    public static class GlobalHelpers
    {
        static HttpServerUtility Server = HttpContext.Current.Server;
        
        
        /// <summary>
        /// Gets "time ago" format string relative to passed date string
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static String GetRelativeTime(String date)
        {
            String result = date;

            if (date.IndexOf("@") >= 0)
            {
                //twitter date
                String[] dateParts = date.Split(' ');
                if (dateParts.Count() >= 6)
                {
                    date = dateParts[1] + " " + dateParts[2] + " " + dateParts[3] + " " + dateParts[5];
                }
            }


            DateTime parsedDate = new DateTime();
            if (DateTime.TryParse(date, out parsedDate))
            {
                TimeSpan difference = DateTime.Now - parsedDate;
                result = DescribeTimeSpan(difference);
            }

            return result;
        }

        /// <summary>
        /// Formats time span as "time ago" string
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string DescribeTimeSpan(TimeSpan t)
        {
            string[] NAMES = {
                            "day",
                            "hour",
                            "minute",
                            "second"
                            };

            int[] ints = {
                         t.Days,
                        t.Hours,
                        t.Minutes,
                        t.Seconds
                      };

            double[] doubles = {
                t.TotalDays,
                t.TotalHours,
                t.TotalMinutes,
                t.TotalSeconds
                };

            var firstNonZero = ints
                .Select((value, index) => new { value, index })
                .FirstOrDefault(x => x.value != 0);
            if (firstNonZero == null)
            {
                return "now";
            }
            int i = firstNonZero.index;
            string prefix = (i >= 3) ? "" : "about ";
            int quantity = (int)Math.Round(doubles[i]);
            return prefix + Tense(quantity, NAMES[i]) + " ago";
        }


        /// <summary>
        /// Replaces url's in text to anchor tags
        /// Sets target as blank and ads nofollow attribute
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static HtmlString ConstructAnchorElements(String text)
        {
            String result = text;

            string urlPattern = "((https?|s?ftp|ssh)\\:/\\/\\[^\"\\s\\<\\>]*[^.,;'\">\\:\\s\\<\\>\\)\\]\\!])/g";
            urlPattern = @"(https?|s?ftp|ssh)\:\/\/[^\" + "\"" + @"\s\<\>]*[^.,;" + "'\"" + @">\:\s\<\>\)\]\!]";
            MatchEvaluator evaluator = new MatchEvaluator(AnchroGenerator);
            result = Regex.Replace(result, urlPattern, evaluator);

            return new HtmlString(result);
        }


        /// <summary>
        /// Gets full url or domain url only if relative url is not specified
        /// </summary>
        /// <param name="relativeUrl"></param>
        /// <returns>http[s]://www.adform.com[relativeUrl] (www.adform.com is example hostname)</returns>
        public static String GetFullUrl(String relativeUrl = "")
        {
            String result = "";

            String port = HttpContext.Current.Request.Url.Port == 80 || HttpContext.Current.Request.Url.Port == 443 ? "" : ":" + HttpContext.Current.Request.Url.Port.ToString();
            result = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + port + relativeUrl;

            return result;
        }

        
        /// <summary>
        /// Gets remote host IP adress
        /// 
        /// NOTE: not tested, taken from stackoverflow
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetClientIp(HttpRequestMessage request) 
        {
            try
            {
                if (request.Properties.ContainsKey("MS_HttpContext"))
                {
                    return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
                }
                else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
                {
                    RemoteEndpointMessageProperty prop;
                    prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                    return prop.Address;
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        /// <summary>
        /// Returns xml nodes list from string containing multiple nodes (with no root element)
        /// </summary>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static IEnumerable<XmlNode> GetXmlNodeList(String xmlString)
        {
            List<XmlNode> result = new List<XmlNode>();

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(String.Format("<root>{0}</root>", xmlString));
                XmlNodeList nodeList = doc.SelectNodes("/root/*");
                if (nodeList != null)
                {
                    foreach (XmlNode node in nodeList)
                    {
                        result.Add(node);
                    }
                }
            }
            catch (Exception e) { }

            return result;
        }


        /// <summary>
        /// gets setting node by name from first site
        /// </summary>
        /// <returns></returns>
        public static IPublishedContent GetSettingNode(string name, UmbracoHelper umbraco = null)
        {
            IPublishedContent result = null;

            var rootNode = GetRootNode(umbraco);

            var settingNode = rootNode.Children.FirstOrDefault(x => x.DocumentTypeAlias == "Settings");

            if (settingNode != null)
            {
                result = settingNode.Children.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            }

            return result;
        }

        /// <summary>
        /// gets library node by name from first site
        /// </summary>
        /// <returns></returns>
        public static IPublishedContent GetLibraryNode(string name, UmbracoHelper umbraco = null)
        {
            IPublishedContent result = null;

            var rootNode = GetRootNode(umbraco);

            var libraryNode = rootNode.Children.FirstOrDefault(x => x.DocumentTypeAlias == "Library");

            if (libraryNode != null)
            {
                result = libraryNode.Children.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            }

            return result;
        }

        /// <summary>
        /// Gets root content node based on current context or just first node of homepage derivative
        /// TODO: think of better implementation of no context fallback
        /// </summary>
        /// <param name="umbraco"></param>
        /// <returns></returns>
        public static IPublishedContent GetRootNode(UmbracoHelper umbraco = null)
        {
            IPublishedContent result = null;

            umbraco = umbraco ?? new UmbracoHelper(UmbracoContext.Current);

            try
            {
                result = umbraco.AssignedContentItem.AncestorOrSelf();
            }
            catch (Exception e)
            {
                var contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
                result = UmbracoContext.Current.ContentCache.GetAtRoot()
                    .FirstOrDefault(x =>
                        contentTypeService.GetContentType(x.DocumentTypeId)
                        .CompositionPropertyTypes
                        .FirstOrDefault(y => 
                            y.Alias == "Homepage") != null
                        ||
                        contentTypeService.GetContentType(x.DocumentTypeId).Alias == "Homepage"
                    );
            }

            return result;
        }


        /// <summary>
        /// gets posts for specified page
        /// </summary>
        /// <param name="postsForPager"></param>
        /// <param name="pageNr"></param>
        /// <param name="postsPerPage"></param>
        /// <returns></returns>
        public static IEnumerable<IPublishedContent> GetPostsForPage(this IEnumerable<IPublishedContent> postsForPager, int pageNr, int postsPerPage)
        {
            IEnumerable<IPublishedContent> pagePosts = postsForPager;

            int startIndex = (pageNr - 1) * postsPerPage;

            return pagePosts.Skip(startIndex).Take(postsPerPage);
        }

        /// <summary>
        /// Gets page number from request url parameters ("p" by default). Returns 1 if not set.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static int GetPageNr(this HttpRequestBase request, string paramName = "p")
        {
            var pageNr = 1;
            Int32.TryParse(request.Params[paramName], out pageNr);
            pageNr = pageNr == 0 ? 1 : pageNr;
            return pageNr;
        }

        /// <summary>
        /// Converts delimetered number string to int list. Usable for converting multiple node fields
        /// </summary>
        /// <param name="text"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static IEnumerable<int> ToIntList(this string text, string separator = ",")
        {
            List<int> result = new List<int>();

            if (!String.IsNullOrEmpty(text))
            {
                var valuesStr = text.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var idStr in valuesStr)
                {
                    int id = -1;
                    if (Int32.TryParse(idStr.Trim(), out id))
                    {
                        result.Add(id);
                    }
                }
            }

            return result;
        }
        
        
        /*******************************************************************************************/
        /*******************************************************************************************/
        /****************************   P R I V A T E  ********************************************/
        /*******************************************************************************************/
        /*******************************************************************************************/


        private static string Tense(int quantity, string noun)
        {
            return quantity == 1
                ? "1 " + noun
                : string.Format("{0} {1}s", quantity, noun);
        }




        private static string AnchroGenerator(Match match)
        {
            String text = match.Value; //can be trimmed for ex.

            return "<a href='" + match.Value + "' target='_blank' rel='nofollow'>" + text + "</a>";
        }

    }
}
