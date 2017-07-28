﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUglify;
using NUglify.JavaScript;

namespace Bundler
{
    /// <summary>
    /// A processor that minifies JavaScript
    /// </summary>
    internal class JavaScriptMinifier : IProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptMinifier"/> class.
        /// </summary>
        public JavaScriptMinifier(CodeSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Gets the custom key that should be used when calculating the memory cache key.
        /// </summary>
        public string CacheKey(HttpContext context) => string.Empty;

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public CodeSettings Settings { get; set; }

        /// <summary>
        /// Executes the processor on the specified configuration.
        /// </summary>
        public Task ExecuteAsync(IAssetContext config)
        {
            return Task.Run(() =>
            {
                UglifyResult minified = Uglify.Js(config.Content, Settings);

                if (!minified.HasErrors)
                {
                    config.Content = minified.Code;
                }
            });
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IAssetPipeline"/>.
    /// </summary>
    public static class JavaScriptMinifierExtensions
    {
        /// <summary>
        /// Runs the JavaScript minifier on the content.
        /// </summary>
        public static IAsset MinifyJavaScript(this IAsset asset)
        {
            return asset.MinifyJavaScript(new CodeSettings());
        }

        /// <summary>
        /// Runs the JavaScript minifier on the content.
        /// </summary>
        public static IAsset MinifyJavaScript(this IAsset asset, CodeSettings settings)
        {
            var minifier = new JavaScriptMinifier(settings);
            asset.Processors.Add(minifier);

            return asset;
        }

        /// <summary>
        /// Runs the JavaScript minifier on the content.
        /// </summary>
        public static IEnumerable<IAsset> MinifyJavaScript(this IEnumerable<IAsset> assets)
        {
            return assets.MinifyJavaScript(new CodeSettings()).ToArray();
        }

        /// <summary>
        /// Runs the JavaScript minifier on the content.
        /// </summary>
        public static IEnumerable<IAsset> MinifyJavaScript(this IEnumerable<IAsset> assets, CodeSettings settings)
        {
            foreach (IAsset asset in assets)
            {
                yield return asset.MinifyJavaScript(settings);
            }
        }
    }
}
