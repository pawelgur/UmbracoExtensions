using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using PG.UmbracoExtensions.Helpers;
using uComponents.DataTypes.UrlPicker.Dto;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace PG.UmbracoExtensions.ClientApi
{
    /// <summary>
    /// These converters are used for serializing different [custom] Umbraco field values depending on their type
    /// 
    /// TODO: use/check core property value converters [package]
    /// TODO: return absolute url for media picker (and maybe other url types)
    /// </summary>
    public static class PropertyValueConverters
    {
        static UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

        public static object DefaultConverter(object value)
        {
            var result = value;

            //try to parse int (numbers will be string in json)
            int id = 0;
            var valueStr = (string) value;
            if (!String.IsNullOrEmpty(valueStr) && !valueStr.Contains(",") && Int32.TryParse(valueStr, out id))
            {
                result = id;
            }

            return result;
        }

        public static object Boolean(object value)
        {
            var result = value;

            //try to parse int (numbers will be string in json)
            var valueStr = (string)value;
            if (!String.IsNullOrEmpty(valueStr))
            {
                result = valueStr == "1";
            }

            return result;
        }
        

        public static object MediaPicker(object value)
        {
            var url = "";
            var valueStr = (string) value;
            if (!String.IsNullOrEmpty(valueStr))
            {
                var mediaNode = umbracoHelper.TypedMedia(valueStr);
                if (mediaNode != null && mediaNode.HasValue("umbracoFile"))
                {
                    url = mediaNode.Url;    
                }
            }
            return url;
        }

       
        public static object MultiNodePicker(object value)
        {
            var result = new List<int>();

            var valueStr = (string) value;

            if (!String.IsNullOrEmpty(valueStr))
            {
                var ids = valueStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in ids)
                {
                    result.Add(Int32.Parse(id));
                }

            }

            return result;
        }

        public static object MultiNodePickerDeep(IEnumerable<IPublishedContent> nodes)
        {
            var result = new List<Dictionary<string, object>>();

            foreach (var node in nodes)
            {
                var serializedNode = ClientApiHelper.GetContentFields(node, false);
                result.Add(serializedNode);
            }
            
            return result;
        }

    
        public static object MultiImagePicker(object value)
        {
            var result = new List<string>();
            var valueStr = (string) value;

            if (!String.IsNullOrEmpty(valueStr))
            {
                var ids = valueStr.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in ids)
                {
                    result.Add((string) MediaPicker(id));
                }
            }

            return result;
        }


        public static object MultiUrlPicker(object value)
        {
            var result = new List<object>();
            var valueStr = (string)value;
            if (!String.IsNullOrEmpty(valueStr))
            {
                var picker = uComponents.DataTypes.MultiUrlPicker.Dto.MultiUrlPickerState.Deserialize(valueStr);
                foreach (var item in picker.Items)
                {
                    result.Add(UrlPickerToDictionary(item));
                }
            }
            return result;
        }
        
        public static object UrlPicker(object value)
        {
            object result = new Dictionary<string, string>();
            var valueStr = (string) value;
            if (!String.IsNullOrEmpty(valueStr))
            {
                var picker = uComponents.DataTypes.UrlPicker.Dto.UrlPickerState.Deserialize(valueStr);
                result = UrlPickerToDictionary(picker);
            }
            return result;
        }


        public static object QrCode(object value, string type = "", int width = 190, int height = 190)
        {
            var result = "";
            var valueStr = (string)value;
            valueStr = valueStr == null ? valueStr : valueStr.Trim();
            if (!String.IsNullOrEmpty(valueStr))
            {
                switch (type)
                {
                    case "Url Picker":
                        var targetUrl = uComponents.DataTypes.UrlPicker.Dto.UrlPickerState.Deserialize(valueStr).Url;
                        result = targetUrl.Trim() == "" ? "" : ImageHelpers.GetQrCodeUrl(targetUrl, width, height);
                        break;
                    default:
                        result = ImageHelpers.GetQrCodeUrl(valueStr, width, height);
                        break;
                }
            }
            return result;
        }

        #region AppSpecificMethods

        public static object CreativeFormatStandardValue(object value, string sourceFieldAlias, IPublishedContent node)
        {
            var result = value;

            switch (sourceFieldAlias)
            {
                case "shortText":
                    result = node.HasValue(sourceFieldAlias)
                        ? value
                        : node.GetTreePickerNode("standardFormat").GetPropertyValue(sourceFieldAlias);
                    break;
                case "thumbnail":
                    result = node.HasValue(sourceFieldAlias)
                        ? MediaPicker(value)
                        : MediaPicker(
                            node.GetTreePickerNode("standardFormat")
                                .GetPropertyValue(sourceFieldAlias));
                    break;
                case "manual":
                    var standardManuals = Creative.CreativeClientApiHelper.GetStandardManual(node);
                    result = Creative.CreativeClientApiHelper.TransformMultiUrlPickerValue(standardManuals);
                    break;
                case "preview":
                    var standardPreviews = Creative.CreativeClientApiHelper.GetStandardPreview(node);
                    result = Creative.CreativeClientApiHelper.TransformMultiUrlPickerValue(standardPreviews);
                    break;
                case "templateAS2":
                case "templateAS3":
                case "templateHTML":
                    var hideAlias = sourceFieldAlias.Replace("template", "hide");
                    var url = node.GetPickerUrl(sourceFieldAlias); //url picker always has value
                    result = !String.IsNullOrEmpty(url) || node.GetPropertyValue<bool>(hideAlias)
                        ? UrlPicker(value)
                        : UrlPicker(
                            node.GetTreePickerNode("standardFormat")
                                .GetPropertyValue(sourceFieldAlias));
                    break;
            }

            return result;
        }


        public static object CreativeFormatPublisherName(IPublishedContent node)
        {
            var result = "";
            if (node != null)
            {
                var publisher = node.AncestorOrSelf("Creative_Publisher");
                result = publisher == null ? "" : publisher.Name;
            }

            return result;

        }

        #endregion


        #region HelperMethods

        public static Dictionary<string, object> UrlPickerToDictionary(UrlPickerState picker)
        {
            var result = new Dictionary<string, object>();
            
            if (picker != null && !String.IsNullOrEmpty(picker.Url))
            {
                var title = String.IsNullOrEmpty(picker.Title) ? "" : picker.Title.Trim();
                
                result = new Dictionary<string, object>
                    {
                        {"url", picker.Url},
                        {"title", title},
                        {"nodeId", picker.NodeId},
                        {"newWindow", picker.NewWindow},
                        {"mode", picker.Mode}
                    };
            }

            return result;
        }


        #endregion
    }
}
