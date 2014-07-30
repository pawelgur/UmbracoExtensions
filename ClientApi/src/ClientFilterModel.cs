using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbraco.Core;
using Umbraco.Core.Models;
using umbraco.editorControls.SettingControls.Pickers;
using umbraco.NodeFactory;
using Umbraco.Web;

namespace PG.UmbracoExtensions.ClientApi
{
    public class ClientFilterModel
    {
        public string fieldAlias { get; set; }
        public string value { get; set; }

    }
}
