using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace PG.UmbracoExtensions.Helpers.UrlPicker
{
    using umbraco;

    using Umbraco.Core.Services;
    using Umbraco.Web;

    /// <summary>
    /// NOTE: taken from uComponents package
    /// 
    /// The DTO which contains the state of a URL picker at any time.
    /// </summary>
    [Serializable]
    public class UrlPickerState
    {
        #region DTO Properties

        /// <summary>
        /// Title for the URL
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Mode that the URL picker is set to.  See UrlPickerMode.
        /// </summary>
        public UrlPickerMode Mode { get; set; }
        /// <summary>
        /// Node ID, if set, for a content node
        /// </summary>
        public int? NodeId { get; set; }
        /// <summary>
        /// URL which is the whole point of this datatype
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Whether the URL is to open in a new window
        /// </summary>
        public bool NewWindow { get; set; }

        #endregion

        /// <summary>
        /// Set defaults
        /// </summary>
        public UrlPickerState()
        {
            Mode = UrlPickerMode.URL;
            NewWindow = false;
        }
        
        /// <summary>
        /// Returns a UrlPickerState based on a serialized string.
        /// 
        /// Tries to infer the format of the serialized data based on known types.  Will throw exceptions
        /// if it fails to parse.
        /// </summary>
        /// <param name="serializedState">An instance of UrlPickerState as a serialized string</param>
        /// <returns>The state</returns>
        public static UrlPickerState Deserialize(string serializedState)
        {
            // Can't deserialize an empty whatever
            if (string.IsNullOrEmpty(serializedState))
            {
                return null;
            }

            // Default
            var state = new UrlPickerState();

            // Imply data format from the formatting of the serialized state
            UrlPickerDataFormat impliedDataFormat;
            if (serializedState.StartsWith("<"))
            {
                impliedDataFormat = UrlPickerDataFormat.Xml;
            }
            else if (serializedState.StartsWith("{"))
            {
                impliedDataFormat = UrlPickerDataFormat.Json;
            }
            else
            {
                impliedDataFormat = UrlPickerDataFormat.Csv;
            }

            // Try to deserialize the string
            try
            {
                switch (impliedDataFormat)
                {
                    case UrlPickerDataFormat.Xml:
                        var dataNode = XElement.Parse(serializedState);

                        // Carefully try to get values out.  This is in case new versions add
                        // to the XML
                        var modeAttribute = dataNode.Attribute("mode");
                        if (modeAttribute != null)
                        {
                            state.Mode = (UrlPickerMode)Enum.Parse(typeof(UrlPickerMode), modeAttribute.Value, false);
                        }

                        var newWindowElement = dataNode.Element("new-window");
                        if (newWindowElement != null)
                        {
                            state.NewWindow = bool.Parse(newWindowElement.Value);
                        }

                        var nodeIdElement = dataNode.Element("node-id");
                        if (nodeIdElement != null)
                        {
                            int nodeId;
                            if (int.TryParse(nodeIdElement.Value, out nodeId))
                            {
                                state.NodeId = nodeId;
                            }
                        }

                        var urlElement = dataNode.Element("url");
                        if (urlElement != null)
                        {
                            state.Url = urlElement.Value;
                        }

                        var linkTitleElement = dataNode.Element("link-title");
                        if (linkTitleElement != null && !string.IsNullOrEmpty(linkTitleElement.Value))
                        {
                            state.Title = linkTitleElement.Value;
                        }

                        break;
                    case UrlPickerDataFormat.Csv:

                        var parameters = serializedState.Split(',');

                        if (parameters.Length > 0)
                        {
                            state.Mode = (UrlPickerMode)Enum.Parse(typeof(UrlPickerMode), parameters[0], false);
                        }
                        if (parameters.Length > 1)
                        {
                            state.NewWindow = bool.Parse(parameters[1]);
                        }
                        if (parameters.Length > 2)
                        {
                            int nodeId;
                            if (int.TryParse(parameters[2], out nodeId))
                            {
                                state.NodeId = nodeId;
                            }
                        }
                        if (parameters.Length > 3)
                        {
                            state.Url = parameters[3].Replace("&#45;", ",");
                        }
                        if (parameters.Length > 4)
                        {
                            if (!string.IsNullOrEmpty(parameters[4]))
                            {
                                state.Title = parameters[4].Replace("&#45;", ",");
                            }
                        }

                        break;
                    case UrlPickerDataFormat.Json:

                        state = JsonConvert.DeserializeObject<UrlPickerState>(serializedState);
                        
                        break;
                    default:
                        throw new NotImplementedException();
                }

                // If the mode is a content node, get the url for the node
                if (state.Mode == UrlPickerMode.Content && state.NodeId.HasValue && UmbracoContext.Current != null)
                {
                    var n = uQuery.GetNode(state.NodeId.Value);
                    var url = n != null ? n.Url : "#";

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        state.Url = url;
                    }

                    if (string.IsNullOrWhiteSpace(state.Title) && n != null)
                    {
                        state.Title = n.Name;
                    }
                }
            }
            catch (Exception)
            {
                // Could not be deserialised, return null
                state = null;
            }

            return state;
        }
    }
}