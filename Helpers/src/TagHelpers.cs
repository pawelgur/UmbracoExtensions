using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using umbraco;
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.Tags;
using umbraco.cms.businesslogic.web;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco;
using Umbraco.Core.Services;

namespace PG.UmbracoExtensions.Helpers
{
    public static class TagHelpers
    {
        /// <summary>
        /// Returns related by tag nodes.
        /// Sorted by score: more tags in common - bigger score. Same score item order is randomized. 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="limit"></param>
        /// <param name="doctypeAlias">filter by document type</param>
        /// <param name="fieldAlias"></param>
        /// <returns></returns>
        public static IEnumerable<IPublishedContent> GetRelatedPosts(this IPublishedContent node, int limit = 10, string doctypeAlias = "", string fieldAlias = "tags")
        {
            var result = new List<IPublishedContent>();

            if (node.HasValue(fieldAlias))
            {
                var scoredList = new SortedDictionary<int, List<IPublishedContent>>();
                var tagsValue = node.GetPropertyValue<string>(fieldAlias);
                var tags = tagsValue.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);

                var relatedPosts = Tag.GetDocumentsWithTags(tagsValue).Where(x => x.Id != node.Id);

                //fill scoredList
                foreach (var post in relatedPosts)
                {
                    if ((!String.IsNullOrEmpty(doctypeAlias) && post.ContentType.Alias == doctypeAlias) || String.IsNullOrEmpty(doctypeAlias))
                    {
                        var relatedNode = UmbracoContext.Current.ContentCache.GetById(post.Id);

                        //calculate score    
                        var postTags = Tag.GetTags(relatedNode.Id);

                        var commonTagCount = 0;
                        foreach (var tag in tags)
                        {
                            if (postTags.FirstOrDefault(x => x.TagCaption == tag) != null)
                            {
                                commonTagCount++;
                            }
                        }


                        var list = scoredList.ContainsKey(commonTagCount)
                            ? scoredList[commonTagCount]
                            : new List<IPublishedContent>();

                        list.Add(relatedNode);

                        scoredList[commonTagCount] = list;
                    }
                }

                //fill results
                foreach (var item in scoredList.OrderByDescending(x => x.Key))
                {
                    var items = item.Value.Count() > 1 
                        ? item.Value.RandomOrder()     //can be replaced with other solution (based on view count, etc.)
                        : item.Value;
                    
                    result.AddRange(items);
                    if (result.Count() >= limit)
                    {
                        break;
                    }
                }
            }

            return result.Take(limit);
        } 
    }
}
