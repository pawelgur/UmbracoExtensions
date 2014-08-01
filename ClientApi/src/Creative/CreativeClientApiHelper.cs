using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PG.UmbracoExtensions.Helpers;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace PG.UmbracoExtensions.ClientApi.Creative
{
    /// <summary>
    /// TODO: All this functionality MUST be removed from ClientApi core and implemented as separate part from ClientApi as it is creative.adform.com specific. !IMPORTANT
    /// </summary>
    public static class CreativeClientApiHelper
    {
        public static bool TestCreativePublisher(IPublishedContent node, PropertyTypeMap propertyTypeMap,
            ClientFilterModel filter)
        {
            var result = true;

            if (node.DocumentTypeAlias == "Creative_Publisher")
            {
                switch (filter.fieldAlias)
                {
                    case "countries":
                        var countries = GetPublisherFormatsFieldIds(node, "country");
                        result = TestMultipleIds(filter.value, countries);
                        break;
                    case "channels":
                        var channels = GetPublisherFormatsFieldIds(node, "channelId");
                        result = TestMultipleIds(filter.value, channels);
                        break;
                    case "devices":
                        var devices = GetPublisherFormatsFieldIds(node, "formatType");
                        result = TestMultipleIds(filter.value, devices);
                        break;
                    case "types":
                        var types = GetPublisherFormatsFieldIds(node, "typeNew");
                        result = TestMultipleIds(filter.value, types);
                        break;
                    case "standardFormatCategories":
                        var standardCategories = GetPublisherStandardCategoriesIds(node);
                        result = TestMultipleIds(filter.value, standardCategories);
                        break;
                    case "standardFormats":
                        var standardFormats = GetPublisherFormatsFieldIds(node, "standardFormat");
                        result = TestMultipleIds(filter.value, standardFormats);
                        break;

                }
            }

            return result;
        }

        public static bool TestMultipleIds(string filterValue, IEnumerable<int> fieldValues)
        {
            var result = true;

            if (!String.IsNullOrEmpty(filterValue) && fieldValues.Any())
            {
                var filterIds = filterValue.ToIntList();
                //check if all filterIds are in field values
                result = !filterIds.Except(fieldValues).Any();
            }

            return result;
        }


        public static IEnumerable<int> GetPublisherFormatsFieldIds(IPublishedContent publisher, string fieldAlias)
        {
            List<int> result = new List<int>();

            if (publisher != null && publisher.DocumentTypeAlias == "Creative_Publisher")
            {
                var formats = publisher.Descendants("Creative_Format");
                foreach (var format in formats)
                {
                    result.AddRange(format.GetIdList(fieldAlias));
                }
            }

            return result.GroupBy(x => x).Select(x => x.First());
        }


        public static IEnumerable<int> GetPublisherStandardCategoriesIds(IPublishedContent node)
        {
            List<int> result = new List<int>();

            var standardFormatIds = GetPublisherFormatsFieldIds(node, "standardFormat");

            foreach (var standardFormatId in standardFormatIds)
            {
                var standardFormat = UmbracoContext.Current.ContentCache.GetById(standardFormatId);
                result.Add(standardFormat.Parent.Id);
            }

            return result.GroupBy(x => x).Select(x => x.First());
        }




        public static Dictionary<String, String> GetStandardManual(IPublishedContent format)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();

            result = format.GetMultiPickerUrl("manual");

            // get standard manuals 
            if (format != null && format.HasProperty("standardFormat"))
            {
                IPublishedContent standardFormat = format.GetTreePickerNode("standardFormat");

                var standardManuals = standardFormat.GetMultiPickerUrl("manual");

                // extend format manuals list with standard ones
                if (!format.GetPropertyValue<bool>("hideManuals"))
                {
                    foreach (var manualLang in standardManuals.Keys)
                    {
                        if (!result.ContainsKey(manualLang))
                        {
                            result[manualLang] = standardManuals[manualLang];
                        }
                    }
                }

            }
            return result;
        }

        public static Dictionary<String, String> GetStandardPreview(IPublishedContent format)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();

            result = format.GetMultiPickerUrl("preview");

            // get standard previews
            if (format != null && format.HasProperty("standardFormat"))
            {
                IPublishedContent standardFormat = format.GetTreePickerNode("standardFormat");

                var standardPreviews = standardFormat.GetMultiPickerUrl("preview");

                // extend with standard previews
                if (!format.GetPropertyValue<bool>("hidePreviews"))
                {
                    foreach (var previewCountry in standardPreviews.Keys)
                    {
                        if (!result.ContainsKey(previewCountry))
                        {
                            result[previewCountry] = standardPreviews[previewCountry];
                        }
                    }
                }

            }

            return result;
        }

        /// <summary>
        /// transforms title:url to "url":[url], "title":[title], used for GetStandardPreview and GetStandardManual conversions
        /// </summary>
        /// <param name="pickerValues"></param>
        /// <returns></returns>
        public static IEnumerable<Dictionary<string, string>> TransformMultiUrlPickerValue(Dictionary<string, string> pickerValues)
        {
            var result = new List<Dictionary<string, string>>();
            foreach (var pickerValue in pickerValues)
            {
                var item = new Dictionary<string, string>
                {
                    {"url", pickerValue.Value.Trim()},
                    {"title", pickerValue.Key.Trim()}
                };
                result.Add(item);
            }

            return result;
        }


    }
}
