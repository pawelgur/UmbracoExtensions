using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Examine.LuceneEngine.SearchCriteria;
using Examine.SearchCriteria;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using umbraco.NodeFactory;
using Umbraco.Web;
using Examine;
using PG.UmbracoExtensions.Helpers;


namespace PG.UmbracoExtensions.ClientApi
{
    public static class ClientApiHelper
    {
        static UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

        /// <summary>
        /// Returns content node fields as dictionary.
        /// Traverses property type map and converts node fields or creates new fields from existing ones based on info from map.
        /// Conversion is done by [PropertyValueConverters] based on [propertyEditorName] from prop type map.
        /// 
        /// NOTE: use deep serialization with caution, as it hits performance
        /// </summary>
        /// <param name="node"></param>
        /// <param name="recursive">enables one level recursion for deep serialization</param>
        /// <param name="propertyTypeMap">pass cached</param>
        /// <returns></returns>
        public static Dictionary<string, object> GetContentFields(IPublishedContent node, bool recursive = true, PropertyTypeMap propertyTypeMap = null)
        {
            //base properties
            var result = new Dictionary<string, object>
            {
                {"name", node.Name},
                {"id", node.Id},
                {"createDate", node.CreateDate},
                {"updateDate", node.UpdateDate},
                {"url", node.Url},
                {"urlName", node.UrlName},
                {"documentTypeAlias", node.DocumentTypeAlias},
                {"documentTypeId", node.DocumentTypeId},
                {"creatorId", node.CreatorId},
                {"creatorName", node.CreatorName}
            };

            result["parentId"] = node.Parent == null ? -1 : node.Parent.Id;
            result["parentName"] = node.Parent == null ? "" : node.Parent.Name;

            //get property type map
            propertyTypeMap = propertyTypeMap ?? PropertyTypeMapHelper.GetCached(node.DocumentTypeId);

            //custom properties
            //TODO: ability to extend/configure (ex: create classes with attributes for property type name)
            //TODO: ability to specify recursion depth
            foreach (var propertyAlias in propertyTypeMap.properties.Keys)
            {
                var propertyOptions = propertyTypeMap.properties[propertyAlias];
                
                //check if it is a new property
                var sourcePropertyAlias = String.IsNullOrEmpty(propertyOptions.sourcePropertyAlias)
                    ? propertyAlias
                    : propertyOptions.sourcePropertyAlias;

                if (node.HasProperty(sourcePropertyAlias))
                {
                    var nodeProperty = node.GetProperty(sourcePropertyAlias);

                    switch (propertyOptions.propertyEditorName)
                    {
                        case "Media Picker":
                            result[propertyAlias] = PropertyValueConverters.MediaPicker(nodeProperty.Value);
                            break;
                        case "Multi-Node Tree Picker":
                            if (recursive &&
                                propertyTypeMap.properties[propertyAlias].serializationOptions ==
                                SerializationOptions.deep)
                            {
                                result[propertyAlias] = PropertyValueConverters.MultiNodePickerDeep(node.GetPropertyValue<IEnumerable<IPublishedContent>>(propertyAlias));
                            }
                            else
                            {
                                result[propertyAlias] = PropertyValueConverters.MultiNodePicker(nodeProperty.Value);
                            }
                            break;
                        case "Multi-Image Picker":
                            result[propertyAlias] = PropertyValueConverters.MultiImagePicker(nodeProperty.Value);
                            break;
                        case "Multi-Url Picker":
                            result[propertyAlias] = PropertyValueConverters.MultiUrlPicker(nodeProperty.Value);
                            break;
                        case "Url Picker":
                            result[propertyAlias] = PropertyValueConverters.UrlPicker(nodeProperty.Value);
                            break;
                        case "QR Code - Url Picker": //qr code from url picker field
                            if (propertyOptions.additionalOptions != null && propertyOptions.additionalOptions.ContainsKey("width") && propertyOptions.additionalOptions.ContainsKey("height"))
                            {
                                var width = (int) propertyOptions.additionalOptions["width"];
                                var height = (int)propertyOptions.additionalOptions["height"];
                                result[propertyAlias] = PropertyValueConverters.QrCode(nodeProperty.Value, "Url Picker", width, height);
                            }
                            else
                            {
                                result[propertyAlias] = PropertyValueConverters.QrCode(nodeProperty.Value, "Url Picker");
                            }
                            break;
                        case "File Download Url":
                            result[propertyAlias] = node.GetDownloadUrl();
                            break;
                        case "Boolean":
                            result[propertyAlias] = PropertyValueConverters.Boolean(nodeProperty.Value);
                            break;
                        //App-specific: creative format standard values
                        case "Creative Format Standard Value":
                            result[propertyAlias] =
                                PropertyValueConverters.CreativeFormatStandardValue(nodeProperty.Value,
                                    sourcePropertyAlias, node);
                            break;
                        //App-specific: creative format publisher name
                        case "Creative Format Publisher Name":
                            result[propertyAlias] =
                                PropertyValueConverters.CreativeFormatPublisherName(node);
                            break;
                        default:
                            result[propertyAlias] = PropertyValueConverters.DefaultConverter(nodeProperty.Value);
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// NOTE: node collection should be one doctype
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static List<object> GetContentCollectionFields(IEnumerable<IPublishedContent> nodes)
        {
            var result = new List<object>();

            var firstNode = nodes.FirstOrDefault();
            var propertyTypeMap = firstNode == null ? null : PropertyTypeMapHelper.GetCached(firstNode.DocumentTypeId);

            foreach (var node in nodes)
            {
                result.Add(GetContentFields(node, true, propertyTypeMap));
            }

            return result;
        }

        
        //TODO: create more elegant solution with totalPostCount
       public static IEnumerable<IPublishedContent> FilterContentNodes(this IEnumerable<IPublishedContent> nodes, ClientRequest request, out int totalPostCount, bool sort = true)
        {
            IEnumerable<IPublishedContent> result = nodes;

            //apply filters
            if (request.filters != null && request.filters.Any())
            {
                var filteredNodes = new List<IPublishedContent>();
                foreach (var node in nodes)
                {
                    var passedFiltering = true;

                    var propertyTypeMap = PropertyTypeMapHelper.GetCached(node.DocumentTypeId);

                    foreach (var filter in request.filters)
                    {
                        if (!ClientFilterController.TestNode(node, propertyTypeMap, filter))
                        {
                            passedFiltering = false;
                            break;
                        }
                    }
                    if (passedFiltering)
                    {
                        filteredNodes.Add(node);
                    }
                }
                result = filteredNodes;
            }

            //apply sorting
            if (sort)
            {
                result = result.SortPosts();
            }

            //apply paging
            totalPostCount = result.Count(); 
            if (request.paged)
            {
                int startIndex = (request.pageNr - 1) * request.pageSize;
                result = result.Skip(startIndex).Take(request.pageSize);
            }


            return result;
        }

        
        /// <summary>
        /// default searching
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static IEnumerable<IPublishedContent> SearchNodes(ClientRequest clientRequest )
        {
            IEnumerable<IPublishedContent> result = new List<IPublishedContent>();

            var providerNameFilter = clientRequest.filters.FirstOrDefault(x => x.fieldAlias == "search_provider");
            var searchStringFilter = clientRequest.filters.FirstOrDefault(x => x.fieldAlias == "search_string"); //node name

            var searchProviderName = providerNameFilter == null ? "" : providerNameFilter.value;
            var searchString = searchStringFilter == null ? "" : searchStringFilter.value.Trim();

            var searcher = String.IsNullOrEmpty(searchProviderName)
                ? ExamineManager.Instance.DefaultSearchProvider
                : ExamineManager.Instance.SearchProviderCollection[searchProviderName];

            IEnumerable<SearchResult> searchResults = null;
            var filters = clientRequest.filters.Where(x => !x.fieldAlias.Contains("search_"));
            if (filters.Any())
            {
                var searchCriteria = searcher.CreateSearchCriteria(BooleanOperation.Or);
                var terms = searchString.Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                var query = searchCriteria.GroupedAnd(GenerateList("nodeName", terms.Count()), terms);
                
                foreach (var filter in filters)
                {
                    switch (filter.fieldAlias)
                    {
                        case "documentTypeAlias" :
                            query = query.And().NodeTypeAlias(filter.value.Boost(0.2f)); //small boost for not related posts not to show
                            break;
                        default:
                            //NOTE: inner fields are searched by their raw value(id vs node name)
                            query = query.And().Field(filter.fieldAlias, filter.value);
                            break;
                    }
                }

                searchResults = searcher.Search(query.Compile()).TakeWhile(x => x.Score > 0.01);
            }
            else
            {
                //simple search
                searchResults = searcher.Search(searchString, false);
            }
            searchResults = searchResults.OrderByDescending(x => x.Score);

            foreach (var searchResult in searchResults)
            {
                var node = umbracoHelper.TypedContent(searchResult.Id);
                if (node != null)
                {
                    ((List<IPublishedContent>) result).Add(node);
                }
            }
            
            
            return result;
        }

        /// <summary>
        /// Serializes content nodes to JSON string which is intended to use for data bootstrapping
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static string SerializeNodesToJson(IEnumerable<IPublishedContent> nodes)
        {
            var serialized_nodes = GetContentCollectionFields(nodes);

            return JsonConvert.SerializeObject(serialized_nodes);
        }


        #region helperHelpers

        /// <summary>
        /// used in search to overcome examine groupedAnd bug (need same number of field names as there are terms)
        /// </summary>
        public static IEnumerable<string> GenerateList(string fieldAlias, int count)
        {
            var result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                result.Add(fieldAlias);
            }

            return result;
        } 

        #endregion

    }
}
