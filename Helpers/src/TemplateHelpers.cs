using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web; 

namespace PG.UmbracoExtensions.Helpers
{
    /// <summary>
    /// Helpers for templates. (generating html, checking parameters, etc.)
    /// </summary>
    public static class TemplateHelpers
    {
        static HttpServerUtility Server = HttpContext.Current.Server;


        /// <summary>
        /// Checks if macro parameter is defined and if it is not blank
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        public static bool ParameterFilled(IDictionary<String, object> parameters, String alias)
        {
            bool filled = false;

            if (parameters != null && alias != null && parameters.ContainsKey(alias) &&
                parameters[alias] != null && parameters[alias].ToString() != "")
            {
                filled = true;

            }

            return filled;
        }



        public static String GetHideMobileCSS(IDictionary<String, object> macroParameters)
        {
            String result = "";
            bool hide = false;
            if (ParameterFilled(macroParameters, "hideMobile"))
            {
                hide = macroParameters["hideMobile"].ToString() == "1";
            }
            result = hide ? "hide-mobile" : "";

            return result;
        }


        /// <summary>
        /// Gets download url for node ([path]/download/[node name])
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetDownloadUrl(this IPublishedContent node)
        {
            return node.Url.Replace(node.UrlName, "download/" + node.UrlName);
        }
    
    
    
    }

  

    /// <summary>
    /// Generates download link for site
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public class HtmlDownloadLink<TModel> : IDisposable
    {
        private readonly HtmlHelper<TModel> _helper;
        const string TAG_NAME = "a";

        public HtmlDownloadLink(HtmlHelper<TModel> helper, IPublishedContent node, Dictionary<string, object> htmlAttributes = null)
        {
            _helper = helper;
            var attrs = new StringBuilder();

            htmlAttributes = htmlAttributes ?? new Dictionary<string, object>();

            htmlAttributes["target"] = "_blank";

            if (node != null)
            {
                var fileUrl = node.GetPickerUrl("fileUrl");

                htmlAttributes["data-url"] = fileUrl;
                htmlAttributes["data-name"] = node.Name;
                htmlAttributes["data-url-name"] = node.UrlName;
                htmlAttributes["data-id"] = node.Id;
                htmlAttributes["data-thumbnail"] = node.HasValue("thumbnail") ?  node.GetThumbnailUrl() : "";

                if (node.HasProperty("hideDownloadForm") && node.GetPropertyValue<bool>("hideDownloadForm") != true)
                {
                    //use download form
                    htmlAttributes["class"] += " js_download-form-trigger";
                    htmlAttributes["href"] = node.GetDownloadUrl(); 
                }
                else
                {
                    htmlAttributes["href"] = fileUrl;
                }
            }

            foreach (var attribute in htmlAttributes)
            {
                attrs.Append(string.Format(" {0}=\"{1}\" ", attribute.Key, attribute.Value));
            }
            

            //write opening tag
            _helper.ViewContext.Writer.Write("<{0}{1}>\n", TAG_NAME, attrs);
        }

        public void Dispose()
        {
            //write closing tag
            _helper.ViewContext.Writer.Write("</{0}>\n", TAG_NAME);
        } 
    }

    public static class HtmlHelperExtensions
    {
        public static HtmlDownloadLink<TModel> BeginDownloadLink<TModel>(this HtmlHelper<TModel> helper, IPublishedContent node, Dictionary<string, object> htmlAttributes = null)
        {
            return new HtmlDownloadLink<TModel>(helper, node, htmlAttributes);
        }
        public static HtmlDownloadLink<TModel> BeginDownloadLink<TModel>(this HtmlHelper<TModel> helper, IPublishedContent node, string className = null)
        {
            var htmlAttributes = className == null ? new Dictionary<string, object>() : new Dictionary<string, object> {{"class", className}};
            return new HtmlDownloadLink<TModel>(helper, node, htmlAttributes);
        }
    }
}
