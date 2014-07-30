using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PG.UmbracoExtensions.ClientApi
{
    /// <summary>
    /// Describes how to convert property. 
    /// 
    /// Can be used to create new properties from existing ones using custom converters.
    /// </summary>
    public class PropertyOptions
    {
        public string propertyEditorName { get; set; }
        public string sourcePropertyAlias { get; set; } //used for generating new properties from existing ones
        public SerializationOptions serializationOptions { get; set; } // [doesn't apply to all property editors]
        public Dictionary<string, object> additionalOptions { get; set; } // ex.: width and height for qr code 

        public PropertyOptions()
        {
            this.serializationOptions = SerializationOptions.flat;
        }

        public PropertyOptions(string propertyEditorName) : this()
        {
            this.propertyEditorName = propertyEditorName;
        }

        public PropertyOptions(string propertyEditorName, string sourcePropertyAlias) : this(propertyEditorName)
        {
            this.sourcePropertyAlias = sourcePropertyAlias;
        }

    }


    public enum SerializationOptions
    {
        deep,  //property will be serialized as content node
        flat   //or as flat id (better performance)
    }

}
