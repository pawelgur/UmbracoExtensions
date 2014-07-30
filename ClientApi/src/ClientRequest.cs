using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace PG.UmbracoExtensions.ClientApi
{
    /// <summary>
    /// Client API model used for defining complex requests (filtering, paging)
    /// </summary>
    public class ClientRequest
    {
        public int nodeId { get; set; }
        
        public bool paged { get; set; }
        public int pageNr { get; set; }
        public int pageSize { get; set; }

        public IEnumerable<ClientFilterModel> filters { get; set; }

        public ClientRequest(){}
    }
}
