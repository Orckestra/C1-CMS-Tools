using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ScrapeForC1CMS.CustomProviders
{
    public interface IContentParser
    {
        /// <summary>
        /// Given a source Uri and the HTML of a page, returns a Page Id. 
        /// It is the responsibility of this method to ensure that - for multi lingual websites, where a common structure is shared across language versions - the same
        /// id is returned for all language versions of the same page.
        /// </summary>
        /// <param name="uri">Source uri for the page being imported</param>
        /// <param name="doc">The HTML for the page being imported</param>
        /// <returns>A Page ID that is shared across language versions for the same page</returns>
        Guid GetCultureInvariantPageId(Uri uri, XDocument doc);

        /// <summary>
        /// Given a source Uri and the HTML of a page, returns a Page Title. 
        /// </summary>
        /// <param name="uri">Source uri for the page being imported</param>
        /// <param name="doc">The HTML for the page being imported</param>
        /// <returns>A Page Title for this page</returns>
        string GetPageTitle(Uri uri, XDocument doc);

        /// <summary>
        /// Given a source Uri, the HTML of a page and a suggestion for a menu title (found by reading link text for links to the uri), returns a Page Menu Title. 
        /// You can typically use the suggestion, provided link texts are good on the source website (you may want to return empty string if suggestion is "Read more" etc).
        /// </summary>
        /// <param name="uri">Source uri for the page being imported</param>
        /// <param name="doc">The HTML for the page being imported</param>
        /// <param name="suggestion">A suggested menu title, found by reading relevant link text</param>
        /// <returns>A Page Menu Title for this page</returns>
        string GetMenuTitle(Uri uri, XDocument doc, string suggestion);

        /// <summary>
        /// Given a source Uri and the HTML of a page, returns a Page Description. If the source website expose descriptions via a meta tag, this text is a good candidate.
        /// </summary>
        /// <param name="uri">Source uri for the page being imported</param>
        /// <param name="doc">The HTML for the page being imported</param>
        /// <returns>A Page Description for this page</returns>
        string GetPageDescription(Uri uri, XDocument doc);

        /// <summary>
        /// Given a source Uri, the HTML of a page and a suggestion for a URL Title (the menu title URL encoded), returns a Page URL Title. 
        /// </summary>
        /// <param name="uri">Source uri for the page being imported</param>
        /// <param name="doc">The HTML for the page being imported</param>
        /// <param name="suggestion">A suggested URL Title, reusing menu title suggestion in a URL safe way</param>
        /// <returns>A Page URL Title for this page</returns>
        string GetPageUrlTitle(Uri uri, XDocument doc, string suggestion);

        /// <summary>
        /// Given a source Uri and the HTML of a page, returns the paceholder content sections (named HTML blocks, that match place holder names in templates) to be imported.
        /// 
        /// This method picks the HTML to include on pages. If the source website has good structure, allowing you to pick blocks like main content, aside content, 
        /// content start (vignette) this detailed breakdown is encouraged.
        /// 
        /// The key used for the returned Dictionary should match the name of a template content placeholder (typically "content", "aside", "start" on C1 CMS starter sites).
        /// </summary>
        /// <param name="uri">Source uri for the page being imported</param>
        /// <param name="doc">The HTML for the page being imported</param>
        /// <returns>Named content for the page</returns>
        Dictionary<string, List<XNode>> GetPlaceholderContent(Uri uri, XDocument doc);

        /// <summary>
        /// Given a source Uri and the HTML of a page, returns the parts of the page containing structued navigation (menus, sub-menus).
        /// 
        /// The imported will put strong emphasis on this when building up the page structure for C1 CMS - when a page is parsed, any new pages found in this retult
        /// will be treated as child pages.
        /// 
        /// The returned HTML will be searched "in depth" so you can safely return a div, containing links further down the DOM.
        /// </summary>
        /// <param name="uri">Source uri for the page being imported</param>
        /// <param name="doc">The HTML for the page being imported</param>
        /// <returns>HTML nodes containing (perhaps deeply) links for structured navigation.</returns>
        IEnumerable<XElement> GetStructuredNavigationElements(Uri uri, XDocument doc);
    }
}
