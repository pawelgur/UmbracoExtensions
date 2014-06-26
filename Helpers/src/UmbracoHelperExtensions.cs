using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Umbraco;
using Umbraco.Web;
using Umbraco.Core.Models;
using System.Web;
using System.Net.Http;

namespace PG.UmbracoExtensions.Helpers
{
    /// <summary>
    /// Extensions for Umbraco helper.
    /// 
    /// Bridge to needed methods.
    /// </summary>
    public static class UmbracoHelperExtensions
    {

        #region GlobalHelpers

        public static String GetRelativeTime(this UmbracoHelper umbraco, String date) 
        {
            return GlobalHelpers.GetRelativeTime(date);
        }

        public static string DescribeTimeSpan(this UmbracoHelper umbraco, TimeSpan t) 
        {
            return GlobalHelpers.DescribeTimeSpan(t);
        }

        public static HtmlString ConstructAnchorElements(this UmbracoHelper umbraco, String text) 
        {
            return GlobalHelpers.ConstructAnchorElements(text);
        }

        public static String GetFullUrl(this UmbracoHelper umbraco, String relativeUrl = "") 
        {
            return GlobalHelpers.GetFullUrl(relativeUrl);
        }


        public static string GetClientIp(this HttpRequestMessage request)
        {
            return GlobalHelpers.GetClientIp(request);
        }

        public static IEnumerable<XmlNode> GetXmlNodeList(this UmbracoHelper umbraco, String xmlString)
        {
            return GlobalHelpers.GetXmlNodeList(xmlString);
        }


        /// <summary>
        /// gets setting node by name from first site
        /// </summary>
        /// <returns></returns>
        public static IPublishedContent GetSettingNode(this UmbracoHelper umbraco, string name)
        {
            return GlobalHelpers.GetSettingNode(name, umbraco);
        }

        /// <summary>
        /// gets library node by name from first site
        /// </summary>
        /// <returns></returns>
        public static IPublishedContent GetLibraryNode(this UmbracoHelper umbraco, string name)
        {
            return GlobalHelpers.GetLibraryNode(name, umbraco);
        }

        #endregion



        #region ImageHelpers

        public static String GetThumbnailUrl(this IPublishedContent post, String cropName = "", bool cropThumb = false) 
        {
            return ImageHelpers.GetThumbnailUrl(post, cropName, cropThumb);
        }

        public static String GetImageUrl(this IPublishedContent post, String cropName = "", String fieldAlias = "image")
        {
            return ImageHelpers.GetImageUrl(post, cropName, fieldAlias);
        }

        public static IEnumerable<String> GetImagesUrl(this IPublishedContent post, String cropName = "", String fieldAlias = "images")
        {
            return ImageHelpers.GetImagesUrl(post, cropName, fieldAlias);
        }

        public static String GetCrop(this UmbracoHelper umbraco, String cropperValue, String croppName)
        {
            return ImageHelpers.GetCrop(cropperValue, croppName);
        }

        public static String GetQrCodeUrl(this UmbracoHelper umbraco, String dataToEncode, int width, int height) 
        {
            return ImageHelpers.GetQrCodeUrl(dataToEncode, width, height);
        }

        #endregion



        #region UmbracoNodeHelpers

        public static String GenerateCSSClass(this IPublishedContent currentNode) 
        {
            return UmbracoNodeHelpers.GenerateCSSClass(currentNode);
        }

        public static String GetPickerUrl(this UmbracoHelper umbraco, String urlPickerValue) 
        {
            return UmbracoNodeHelpers.GetPickerUrl(urlPickerValue);
        }

        public static Dictionary<String, String> GetMultiPickerUrl(this UmbracoHelper umbraco, String urlPickerValue) 
        {
            return UmbracoNodeHelpers.GetMultiPickerUrl(urlPickerValue);
        }

        public static bool IsCategory(this IPublishedContent node)
        {
            return UmbracoNodeHelpers.IsCategory(node);
        }

        #endregion


        #region TemplateHelpers

        public static bool ParameterFilled(this UmbracoHelper umbraco, IDictionary<String, object> parameters, String alias) 
        {
            return TemplateHelpers.ParameterFilled(parameters, alias);
        }

        public static String GetHideMobileCSS(this UmbracoHelper umbraco, IDictionary<String, object> macroParameters) 
        {
            return TemplateHelpers.GetHideMobileCSS(macroParameters);
        }

        #endregion

        #region SortingHelpers

        public static IEnumerable<IPublishedContent> SortPosts(this IEnumerable<IPublishedContent> posts)
        {
            return SortingHelper.SortPosts(posts);
        }

        #endregion

    }
}
