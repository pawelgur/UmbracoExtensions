using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace PG.UmbracoExtensions.Helpers.LegacyUrlPicker
{
    /// <summary>
    /// NOTE: taken from uComponents
    /// 
    /// The DTO which contains the state of a Multi URL picker at any time.
    /// </summary>
    [Serializable]
    public class MultiUrlPickerState
    {
        /// <summary>
        /// The items created
        /// </summary>
        public IEnumerable<UrlPickerState> Items { get; set; }

        /// <summary>
        /// Sets defaults
        /// </summary>
        public MultiUrlPickerState()
        {
            Items = new List<UrlPickerState>();
        }

        /// <summary>
        /// Returns a MultiUrlPickerState based on a serialized string.
        /// 
        /// Tries to infer the format of the serialized data based on known types.  Will throw exceptions
        /// if it fails to parse.
        /// </summary>
        /// <param name="serializedState">An instance of MultiUrlPickerState as a serialized string</param>
        /// <returns>The state</returns>
        public static MultiUrlPickerState Deserialize(string serializedState)
        {
            // Can't deserialize an empty whatever
            if (string.IsNullOrEmpty(serializedState))
            {
                return null;
            }

            // Default
            var state = new MultiUrlPickerState();
            var items = new List<UrlPickerState>();

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

                        // Get each url-picker node
                        var dataNode = XElement.Parse(serializedState);
                        var xmlItems = dataNode.Elements("url-picker");

                        foreach (var xmlItem in xmlItems)
                        {
                            // Deserialize it
                            items.Add(UrlPickerState.Deserialize(xmlItem.ToString()));
                        }

                        state.Items = items;

                        break;
                    case UrlPickerDataFormat.Csv:

                        // Split CSV by lines
                        var csvItems = serializedState.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        // Deserialize each line
                        foreach (var csvItem in csvItems)
                        {
                            items.Add(UrlPickerState.Deserialize(csvItem));
                        }

                        state.Items = items;

                        break;
                    case UrlPickerDataFormat.Json:

                        state = JsonConvert.DeserializeObject<MultiUrlPickerState>(serializedState);

                        break;
                    default:
                        throw new NotImplementedException();
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
