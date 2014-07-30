using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace PG.UmbracoExtensions.ClientApi
{
    /// <summary>
    /// Represents Umbraco document type custom fields and their types for correct typed 
    /// value serialization. (ex: serialize media picker as fileUrl and not media node id)
    /// 
    /// Should be used as centralised solution to any doctype field type analysis.
    /// 
    /// NOTE: creating one is slow, use caching
    /// 
    /// TODO: create config file to read from;
    /// </summary>
    public class PropertyTypeMap
    {
        public int documentTypeId { get; set; }
        public Dictionary<string, PropertyOptions> properties { get; set; } // [propertyAlias]:[propertyOptions object]

        public PropertyTypeMap(int documentTypeId)
        {
            this.documentTypeId = documentTypeId;

            FillProperties();
        }


        void FillProperties()
        {
            properties = new Dictionary<string, PropertyOptions>();

            var doctype = ApplicationContext.Current.Services.ContentTypeService.GetContentType(documentTypeId);
            foreach (var propertyType in doctype.CompositionPropertyTypes)
            {
                //Mapping by property editor name
                var propertyEditor = ApplicationContext.Current.Services.DataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);
                if (propertyEditor != null)
                {
                    //TODO: read Datatype to PropertyEditor mapping from config file
                    switch (propertyEditor.Name)
                    {
                        case "Image Picker":
                        case "Media Picker":
                            properties[propertyType.Alias] = new PropertyOptions("Media Picker");
                            break;
                        case "Multi-Node Picker":
                        case "Format Type Picker":   //this should be in siteUmbraco specific config file
                        case "Presenter Picker":    //this should be in siteUmbraco specific config file
                        case "Creative Channel Node Picker": //this should be in creative specific config file
                        case "Creative Format Type Picker":  //this should be in creative specific config file
                            properties[propertyType.Alias] = new PropertyOptions("Multi-Node Tree Picker");
                            if (doctype.Alias == "Site_CreativeSpacePost")
                            {
                               properties[propertyType.Alias].serializationOptions = SerializationOptions.deep;
                            }
                            break;
                        case "CS Post Picker Multiple": //this should be in siteUmbraco specific config file
                            properties[propertyType.Alias] = new PropertyOptions("Multi-Node Tree Picker");
                            if (doctype.Alias == "Site_RichMediaFormat")
                            {
                               properties[propertyType.Alias].serializationOptions = SerializationOptions.deep;
                            }
                            break;;
                        case "Country Picker":
                            properties[propertyType.Alias] = new PropertyOptions("Multi-Node Tree Picker");
                            if (doctype.Alias == "Site_CreativeSpacePost")
                            {
                                properties[propertyType.Alias].serializationOptions = SerializationOptions.deep;
                            }
                            if (doctype.Alias == "Creative_Contact")
                            {
                                properties[propertyType.Alias].serializationOptions = SerializationOptions.deep;
                            }
                            break;
                        case "Case Study Picker":
                            properties[propertyType.Alias] = new PropertyOptions("Multi-Node Tree Picker");
                            if (doctype.Alias == "Site_CreativeSpacePost" || doctype.Alias == "Site_RichMediaFormat")
                            {
                               properties[propertyType.Alias].serializationOptions = SerializationOptions.deep;
                            }
                            break;
                        case "Multi-Image Picker":
                            properties[propertyType.Alias] = new PropertyOptions("Multi-Image Picker");
                            break;
                        case "Multi-Url Picker":
                            properties[propertyType.Alias] = new PropertyOptions("Multi-Url Picker");
                            break;
                        case "Url Picker":
                        case "Url Picker - Url, Media":
                            properties[propertyType.Alias] = new PropertyOptions("Url Picker");
                            break;
                        case "True/false":
                            properties[propertyType.Alias] = new PropertyOptions("Boolean");
                            break;
                        default:
                            properties[propertyType.Alias] = new PropertyOptions("");
                            break;
                    }
                }
                else
                {
                    properties[propertyType.Alias] = new PropertyOptions("");
                }
            }
            //additional custom generated properties
            GenerateCustomProperties(doctype);
            
        }

        /// <summary>
        /// Add custom properties to doctype. This should be overriden for app-specific fields
        /// </summary>
        /// <param name="doctype"></param>
        public virtual void GenerateCustomProperties(IContentType doctype)
        {
            //Mapping based on doctype alias
            switch (doctype.Alias)
            {
                //SiteUmbraco custom type properties
                case "Site_CreativeSpacePost":
                case "Site_RichMediaFormat":
                    var propertyOptions = new PropertyOptions("QR Code - Url Picker", "mobilePreview");
                    propertyOptions.additionalOptions = new Dictionary<string, object>
                    {
                        {"width", 190},
                        {"height", 190}
                    };
                    properties["mobilePreviewQrCode"] = propertyOptions;
                    break;
                case "Site_CaseStudy":
                case "Site_Collateral":
                    var propertyOptions1 = new PropertyOptions("File Download Url", "fileUrl");
                    properties["downloadUrl"] = propertyOptions1;
                    break;
                //Creative specific property converters based on field alias
                case "Creative_Format":
                    //set standard format properties
                    properties["manual"] = new PropertyOptions("Creative Format Standard Value", "manual");
                    properties["preview"] = new PropertyOptions("Creative Format Standard Value", "preview");
                    properties["thumbnail"] = new PropertyOptions("Creative Format Standard Value", "thumbnail");
                    properties["shortText"] = new PropertyOptions("Creative Format Standard Value", "shortText");
                    properties["templateAS2"] = new PropertyOptions("Creative Format Standard Value", "templateAS2");
                    properties["templateAS3"] = new PropertyOptions("Creative Format Standard Value", "templateAS3");
                    properties["templateHTML"] = new PropertyOptions("Creative Format Standard Value", "templateHTML");
                    properties["publisherName"] = new PropertyOptions("Creative Format Publisher Name", "standardFormat"); //standardFormat will be unused here
                    break;
            }
        }


    }


    

    public static class PropertyTypeMapHelper
    {
        /// <summary>
        /// gets cached PropertyTypeMap or creates new and caches it if needed
        /// 
        /// TODO: create dependency on config file (when it will be created)
        /// </summary>
        /// <param name="doctypeId"></param>
        /// <returns></returns>
        public static PropertyTypeMap GetCached(int doctypeId)
        {
            var cacheKey = "clientapi_property-type-map-" + doctypeId;

            var map = (PropertyTypeMap) HttpRuntime.Cache[cacheKey];
            if (map == null)
            {
                map = new PropertyTypeMap(doctypeId);
                HttpRuntime.Cache.Insert(cacheKey, map, null,  DateTime.Now.AddHours(2), System.Web.Caching.Cache.NoSlidingExpiration);
            }

            return map; 
        }

    }
}
