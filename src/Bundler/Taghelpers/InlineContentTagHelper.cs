﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace Bundler.Taghelpers
{

    /// <summary>
    /// Tag helper for inlining CSS
    /// </summary>
    [HtmlTargetElement("link", Attributes = "inline, href")]
    [HtmlTargetElement("script", Attributes = "inline, src")]
    public class InlineContentTagHelper : BaseTagHelper
    {
        private string _route;

        /// <summary>
        /// Tag helper for inlining content
        /// </summary>
        public InlineContentTagHelper(IHostingEnvironment env, IMemoryCache cache, IAssetPipeline pipeline)
            : base(env, cache, pipeline)
        { }

        /// <summary>
        /// Makes sure this taghelper runs before the built in ones.
        /// </summary>
        public override int Order => base.Order + 1;

        /// <summary>
        /// Creates a tag helper for inlining content
        /// </summary>
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (output.TagName.Equals("link", StringComparison.OrdinalIgnoreCase))
            {
                output.TagName = "style";
                ParseRoute(context.AllAttributes["href"].Value.ToString());
                output.Attributes.Clear();
            }
            else if (output.TagName.Equals("script", StringComparison.OrdinalIgnoreCase))
            {
                ParseRoute(context.AllAttributes["src"].Value.ToString());
                output.Attributes.RemoveAll("inline");
                output.Attributes.RemoveAll("integrity");
                output.Attributes.RemoveAll("language");
                output.Attributes.RemoveAll("src");
                output.Attributes.RemoveAll("async");
                output.Attributes.RemoveAll("defer");
            }

            string content = await GetFileContentAsync(_route);

            output.Content.SetHtmlContent(content);
            output.TagMode = TagMode.StartTagAndEndTag;
        }

        private void ParseRoute(string route)
        {
            int index = route.IndexOfAny(new[] { '?', '#' });

            if (index > -1)
            {
                _route = route.Substring(0, index);
            }
            else
            {
                _route = route;
            }
        }

        private async Task<string> GetFileContentAsync(string route)
        {
            IAsset asset = Pipeline.FromRoute(route);
            string cacheKey = asset == null ? route : asset.GenerateCacheKey(ViewContext.HttpContext);

            if (Cache.TryGetValue(cacheKey, out string value))
            {
                return value;
            }

            if (asset != null)
            {
                string contents = await asset.ExecuteAsync(ViewContext.HttpContext);

                AddToCache(cacheKey, contents, asset.SourceFiles.ToArray());
                return contents;
            }
            else
            {
                Pipeline.EnsureDefaults(HostingEnvironment);

                string file = Pipeline.FileProvider.GetFileInfo(route).PhysicalPath;

                if (File.Exists(file))
                {
                    string contents = File.ReadAllText(file);
                    AddToCache(cacheKey, contents, file);

                    return contents;
                }
            }

            throw new FileNotFoundException("File or bundle doesn't exist", route);
        }

        private void AddToCache(string cacheKey, string value, params string[] files)
        {
            var cacheOptions = new MemoryCacheEntryOptions();

            foreach (string file in files)
            {
                cacheOptions.AddExpirationToken(Pipeline.FileProvider.Watch(file));
            }

            Cache.Set(cacheKey, value, cacheOptions);
        }
    }
}
