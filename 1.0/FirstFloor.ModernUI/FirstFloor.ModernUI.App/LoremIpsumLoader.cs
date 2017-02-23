using FirstFloor.ModernUI.App.Content;
using FirstFloor.ModernUI.Windows;
using System;

namespace FirstFloor.ModernUI.App
{
    /// <summary>
    /// Loads lorem ipsum content regardless the given uri.
    /// </summary>
    public class LoremIpsumLoader
        : DefaultContentLoader
    {
        /// <summary>
        /// Loads the content from specified uri.
        /// </summary>
        /// <param name="uri">The content uri</param>
        /// <returns>The loaded content.</returns>
        protected override object LoadContent(Uri uri)
        {
            // return a new LoremIpsum user control instance no matter the uri
            return new LoremIpsum();
        }
    }
}
