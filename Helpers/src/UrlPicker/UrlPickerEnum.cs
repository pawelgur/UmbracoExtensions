using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PG.UmbracoExtensions.Helpers.UrlPicker
{

    /// <summary>
    /// NOTE: taken from uComponents
    /// 
    /// The modes this datatype can implement - they refer to how the local/external content is referred.
    /// </summary>
    public enum UrlPickerMode : int
    {
        /// <summary>
        /// URL string
        /// </summary>
        URL = 1,
        /// <summary>
        /// Content node
        /// </summary>
        Content = 2,
        /// <summary>
        /// Media node
        /// </summary>
        Media = 3,
        /// <summary>
        /// Upload a file
        /// </summary>
        Upload = 4
    }

    /// <summary>
    /// Determines in which serialized format the the data is saved to the database
    /// </summary>
    public enum UrlPickerDataFormat
    {
        /// <summary>
        /// Store as XML
        /// </summary>
        Xml,
        /// <summary>
        /// Store as comma delimited (CSV, single line)
        /// </summary>
        Csv,
        /// <summary>
        /// Store as a JSON object, which can be deserialized by .NET or JavaScript
        /// </summary>
        Json
    }
}
