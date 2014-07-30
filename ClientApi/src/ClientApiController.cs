using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Newtonsoft.Json;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.WebApi;
using PG.UmbracoExtensions.Helpers;

namespace PG.UmbracoExtensions.ClientApi
{
    /// <summary>
    /// TODO: cache requests(atleast paged/unfiltered)
    /// </summary>
    public class ClientApiController : UmbracoApiController
    {
        private readonly UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

        public object GetNode(int? id = 0)
        {
            var node = umbracoHelper.TypedContent(id);
            return ClientApiHelper.GetContentFields(node);
        }

        public object GetParent(int? id = 0)
        {
            var node = umbracoHelper.TypedContent(id).Parent;
            return ClientApiHelper.GetContentFields(node); ;
        }

        [BindJson(typeof(ClientRequest), "request")]
        public object GetChildren(ClientRequest request)
        {
            if (ModelState.IsValid && request != null)
            {
                var nodes = umbracoHelper.TypedContent(request.nodeId).Children();
                int totalNodes = 0; //needed for paging 
                nodes = nodes.FilterContentNodes(request, out totalNodes); //WARNING: slow!
                HttpContext.Current.Response.AppendHeader("Total-Post-Count", totalNodes.ToString());
                return ClientApiHelper.GetContentCollectionFields(nodes);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState);
            }
        }

        [BindJson(typeof(ClientRequest), "request")]
        public object GetDescendants(ClientRequest request)
        {
            if (ModelState.IsValid && request != null)
            {
                var nodes = umbracoHelper.TypedContent(request.nodeId).Descendants(); 
                int totalNodes = 0; //needed for paging 
                nodes = nodes.FilterContentNodes(request, out totalNodes); //WARNING: slow!
                HttpContext.Current.Response.AppendHeader("Total-Post-Count", totalNodes.ToString());
                return ClientApiHelper.GetContentCollectionFields(nodes); ;
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState);
            }
        }

        [System.Web.Http.HttpGet]
        [BindJson(typeof(ClientRequest), "request")]
        public object SearchNodes(ClientRequest request)
        {
            if (ModelState.IsValid && request != null)
            {
                IEnumerable<IPublishedContent> nodes = new List<IPublishedContent>();
                nodes = ClientApiHelper.SearchNodes(request);
                //page
                int totalNodes = nodes.Count();
                if (request.paged)
                {
                    nodes = nodes.GetPostsForPage(request.pageNr, request.pageSize);
                }
                HttpContext.Current.Response.AppendHeader("Total-Post-Count", totalNodes.ToString());

                return ClientApiHelper.GetContentCollectionFields(nodes);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState);
            }
        }
        

    }

    public class BindJson : System.Web.Http.Filters.ActionFilterAttribute
    {
        Type _type;
        string _queryStringKey;
        public BindJson(Type type, string queryStringKey)
        {
            _type = type;
            _queryStringKey = queryStringKey;
        }
        public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var json = actionContext.Request.RequestUri.ParseQueryString()[_queryStringKey];
            actionContext.ActionArguments[_queryStringKey] = JsonConvert.DeserializeObject(json, _type);
        }
    }

}
