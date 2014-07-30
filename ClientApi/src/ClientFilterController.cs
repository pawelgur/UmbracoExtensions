using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PG.UmbracoExtensions.ClientApi.Creative;
using Umbraco.Core;
using Umbraco.Core.Models;
using umbraco.editorControls.SettingControls.Pickers;
using umbraco.NodeFactory;
using Umbraco.Web;

namespace PG.UmbracoExtensions.ClientApi
{
    /// <summary>
    /// filter class used for all field-specific filtering logic
    /// NOTE: supports nested fields (nodeObjectFieldAlias.targetNodeFieldAlias, ex.: standardFormat.parentId)
    /// TODO: custom operators
    /// </summary>
    public static class ClientFilterController
    {
        //main filtering logic goes here
        public static bool TestNode(IPublishedContent node, PropertyTypeMap propertyTypeMap, ClientFilterModel filter)
        {
            var result = true;

            //core fields
            var isCoreField = true;
            result = TestCoreFields(node, propertyTypeMap, filter, out isCoreField);

            //custom fields
            if (!isCoreField)
            {
                //node properties
                if (node.HasProperty(filter.fieldAlias))
                {
                    result = TestCustomFields(node, propertyTypeMap, filter);
                }
                //deeper fields
                //TODO: multiple node fields 
                else if (filter.fieldAlias.Contains("."))
                {
                    var currentFieldAlias =
                        filter.fieldAlias.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    var deepFieldAlias = filter.fieldAlias.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)[1];

                    IPublishedContent targetNode = null;
                    //parent
                    if (currentFieldAlias == "parent")
                    {
                        targetNode = node.Parent;
                    }
                    //custom field
                    else if (node.HasValue(currentFieldAlias))
                    {
                        var value =
                            ((string)node.GetProperty(currentFieldAlias).DataValue).Split(new char[] { ',' },
                                StringSplitOptions.RemoveEmptyEntries)[0];
                        var umbracoHelper = new UmbracoHelper(UmbracoContext.Current, node);
                        targetNode = umbracoHelper.TypedContent(value);
                    }
                    if (targetNode != null)
                    {
                        //perform recursive test
                        var deepFilter = new ClientFilterModel
                        {
                            fieldAlias = deepFieldAlias,
                            value = filter.value
                        };
                        var deepTypeMap = PropertyTypeMapHelper.GetCached(targetNode.DocumentTypeId);
                        result = TestNode(targetNode, deepTypeMap, deepFilter);
                    }
                }
                //additional testing (doctype-specific fields not present in node)
                else
                {
                    result = TestNotExistingProperties(node, propertyTypeMap, filter);
                }
            }
            

            return result;
        }

        static bool TestCoreFields(IPublishedContent node, PropertyTypeMap propertyTypeMap, ClientFilterModel filter, out bool isCoreField)
        {
            var result = true;
            isCoreField = true;

            switch (filter.fieldAlias)
            {
                case "name":
                    result = TestString(node.Name, filter.value);
                    break;
                case "id":
                    result = TestInt(node.Id, filter.value);
                    break;
                case "parentId":
                    result = node.Parent != null && TestInt(node.Parent.Id, filter.value);
                    break;
                case "createDate":
                    result = TestDate(node.CreateDate, filter.value);
                    break;
                case "updateDate":
                    result = TestDate(node.UpdateDate, filter.value);
                    break;
                case "url":
                    result = TestString(node.Url, filter.value);
                    break;
                case "urlName":
                    result = TestString(node.UrlName, filter.value);
                    break;
                case "documentTypeAlias":
                    result = TestString(node.DocumentTypeAlias, filter.value);
                    break;
                case "documentTypeId":
                    result = TestInt(node.DocumentTypeId, filter.value);
                    break;
                case "creatorId":
                    result = TestInt(node.CreatorId, filter.value);
                    break;
                case "creatorName":
                    result = TestString(node.CreatorName, filter.value);
                    break;
                default:
                    isCoreField = false;
                    break;
            }

            return result;
        }

        static bool TestCustomFields(IPublishedContent node, PropertyTypeMap propertyTypeMap, ClientFilterModel filter)
        {
            var result = true;

            switch (propertyTypeMap.properties[filter.fieldAlias].propertyEditorName)
            {
                case "Media Picker":
                    result = TestInt(node.GetPropertyValue<int>(filter.fieldAlias), filter.value);
                    break;
                case "Multi-Node Tree Picker":
                    result = TestMultipleNodes((string)node.GetProperty(filter.fieldAlias).DataValue, filter.value);
                    break;
                default:
                    result = TestString((string)node.GetProperty(filter.fieldAlias).DataValue, filter.value); //GetProperty().DataValue won't trigger value converters -> gets raw id
                    break;
            }

            return result;
        }

        /// <summary>
        /// Used for more advanced scenarios, as traversing children or similar. Custom code.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="propertyTypeMap"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        static bool TestNotExistingProperties(IPublishedContent node, PropertyTypeMap propertyTypeMap, ClientFilterModel filter)
        {
            var result = true;

            //creative.adform.com publisher testing
            //TODO: move to external lib
            result = CreativeClientApiHelper.TestCreativePublisher(node, propertyTypeMap, filter);

            return result;
        }

        #region typeTesters

        static bool TestString(string nodeValue, string filterValue)
        {
            return filterValue.Trim() == nodeValue.Trim();
        }

        static bool TestInt(int nodeValue, string filterValue)
        {
            return Int32.Parse(filterValue) == nodeValue;
        }

        static bool TestDate(DateTime nodeValue, string filterValue)
        {
            return DateTime.Parse((string)filterValue) == nodeValue;
        }

        static bool TestMultipleNodes(string nodeIdsStr, string filterValue)
        {
            var values = filterValue.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var nodeIds = nodeIdsStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            var containsAllValues = true;
            foreach (var idStr in values)
            {
                var id = Int32.Parse(idStr.Trim());
                if (nodeIds.FirstOrDefault(x => Int32.Parse(x.Trim()) == id) == null)
                {
                    containsAllValues = false;
                    break;
                }
            }


            return containsAllValues;
        }

        #endregion

    }
}
