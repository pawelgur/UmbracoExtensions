using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbraco;
using Umbraco.Core.Models;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web;
using System.Text.RegularExpressions;
using System.Web;

namespace PG.UmbracoExtensions.Helpers
{
    public static class SortingHelper
    {
        //TODO: cache sorted id's (pass some kind of cache key)
        public static IEnumerable<IPublishedContent> SortPosts(IEnumerable<IPublishedContent> posts)
        {
            IEnumerable<IPublishedContent> result = posts;

            if (posts != null && posts.Count() > 1)
            {
                IPublishedContent firstPost = posts.First();
                IPublishedContent configurationNode = HasSortingConfiguration(firstPost.Parent)  ? firstPost.Parent : firstPost.Parent.Parent; //try ancestor for configuration

                if (HasSortingConfiguration(configurationNode))
                {
                    var sortByVal = configurationNode.GetPropertyValue("sortBy");
                    var sortOrderVal = configurationNode.GetPropertyValue("sortOrderNew");
                    var sortBy = sortByVal == "" ? "Sort date" : umbraco.library.GetPreValueAsString(Convert.ToInt32(sortByVal));
                    var sortOrder = sortOrderVal == "" ? "Descending" : umbraco.library.GetPreValueAsString(Convert.ToInt32(configurationNode.GetPropertyValue("sortOrderNew")));

                    switch (sortBy)
                    {
                        case "Sort date":
                            if (firstPost.HasProperty("sortDate"))
                            {
                                result = result.OrderBy(x => x.GetPropertyValue("sortDate"));
                            }
                            break;
                        case "Update date":
                            result = result.OrderBy(x => x.UpdateDate);
                            break;
                        case "CMS order":
                            sortOrder = "";
                            break;
                    }

                    if (sortOrder == "Descending")
                    {
                        result = result.Reverse();
                    }

                }
                else
                {
                    //no parent sorting settings, default sorting by "Sort date" 
                    if (firstPost.HasProperty("sortDate"))
                    {
                        result = result.OrderBy(x => x.GetPropertyValue("sortDate"));
                    }
                }
            }

            return result;
        }


        static bool HasSortingConfiguration(IPublishedContent node)
        {
            return node != null && node.HasProperty("sortBy") && node.HasProperty("sortOrderNew");
        }
    }
}
