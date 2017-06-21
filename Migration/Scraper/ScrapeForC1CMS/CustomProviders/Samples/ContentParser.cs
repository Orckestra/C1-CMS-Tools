using ScrapeForC1CMS;
using ScrapeForC1CMS.CustomProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ScrapeForC1CMS.CustomProviders.Samples
{
    class ContentParser : IContentParser
    {
        private XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// If you are importing a multilingual website it is your responsibility to ensure that the same GUID is returned for different language versions of the same page
        /// In this demo, the URLs are identical for the two language versions, except "/chinese" is injected in the URL for the Chinese version. So this is easy.
        /// If you do not have this option, you should either expose a common ID in the source website html head and red id from the doc parameter or have a supporting
        /// data structure, that map uri to a GUID.
        /// 
        /// If different GUIDs are returned for the "same page" multiple pages will be created in C1 CMS. So for a one-to-one structure, you need to fix this... 
        /// </summary>
        /// <returns></returns>
        public Guid GetCultureInvariantPageId(Uri uri, XDocument doc)
        {
            string invariantPath = uri.ToString().Replace("/chinese/", "/"); 
            return Utils.GetGuidFromstring(invariantPath);
        }


        public string GetPageTitle(Uri uri, XDocument doc)
        {
            var titleElements = doc.Root.Element(xhtmlNs + "head").Elements(xhtmlNs + "title");
            return (titleElements.Count() > 0 ? titleElements.First().Value : "*** NO TITLE FOUND IN SOURCE ***");
        }


        public string GetMenuTitle(Uri uri, XDocument doc, string suggestion)
        {
            // if pages are linked via content with some generic string, like 'read more', this is the place to avoid this becomming the menu title:
            if (suggestion.ToLower() == "read more" || suggestion.ToLower() == "lire la suite") return "";
            return suggestion;
        }


        public string GetPageDescription(Uri uri, XDocument doc)
        {
            var headElements = doc.Root.Element(xhtmlNs + "head").Elements();
            var descriptionElements = headElements.Where(f => (string)f.Attribute(xhtmlNs + "name") == "description");
            return descriptionElements.Select(f => (string)f.Attribute(xhtmlNs + "content")).FirstOrDefault();
        }


        public string GetPageUrlTitle(Uri uri, XDocument doc, string suggestion)
        {
            string[] pathArray = uri.AbsolutePath.Split('/');
            string query = uri.Query.Replace("?", "").Replace("=", "-").Replace("&", "-");

            return pathArray.Last() + query;
        }


        public Dictionary<string, List<XNode>> GetPlaceholderContent(Uri uri, XDocument doc)
        {
            return GetPlaceholderContents(uri, doc);
        }


        /// <summary>
        /// Return all elements that wrap structured navigation on the page (doc) handed to you
        /// </summary>
        public IEnumerable<XElement> GetStructuredNavigationElements(Uri uri, XDocument doc)
        {
            // yield return GetDivById(doc, "menu");
            foreach (var element in GetElementsByClass(doc, "leftmenu"))
                yield return element;

            foreach (var element in GetElementsByClass(doc, "leftmenusubmenu"))
                yield return element;
        }




        // concrete functions


        /// <summary>
        /// Here you need to locate the content blocks that should go into C1 CMS pages - if the source website has good id/class markup, this is relatively easy.
        /// 
        /// The dictionary returned should contain "placeholder name" and "html". For the standard C1 CMS starter sites, the following placeholder names are used:
        ///  - start (top of content, like a heading or intro - can be empty)
        ///  - content (the main content of the page)
        ///  - aside (if there is an aside column)
        ///  
        /// If you don't have content for a placeholder, simply do not return it. The template chooser can select the right template, based on what you return here.
        /// (for example, if you do not return an aside, then the template chooser can pick a template without an aside).
        /// </summary>
        private Dictionary<string, List<XNode>> GetPlaceholderContents(Uri uri, XDocument doc)
        {
            var result = new Dictionary<string, List<XNode>>();

            var middleAreaElement = GetElementsByClass(doc, "middlearea").FirstOrDefault();

            if (middleAreaElement == null)
            {
                return null;
            }

            var startElements = GetElementsByClass(middleAreaElement, "middleheader").Nodes().ToList();
            if (startElements.Any())
            {
                result.Add("start", startElements);
            }

            var contentElements = new List<XNode>();
            foreach (var element in middleAreaElement.Elements())
            {
                string className = (string)element.Attribute("class");

                if (className!= "breadcrumbarea" && className != "middleheader")
                {
                    contentElements.AddRange(element.Nodes());
                }
            }

            if (contentElements.Any())
            {
                result.Add("content", contentElements);
            }

            var rightAreaElement = GetElementsByClass(doc, "rightarea").FirstOrDefault();

            if (rightAreaElement!=null && rightAreaElement.Elements().Any())
            {
                var asideElements = new List<XNode>();
                asideElements.AddRange(rightAreaElement.Elements().Nodes());
            }

            return result;
        }


        private XElement GetElementById(XContainer source, string id)
        {
            return source.Descendants().Where(f => (string)f.Attribute("id") == id).FirstOrDefault();
        }
        private IEnumerable<XElement> GetElementsByClass(XContainer source, string className)
        {
            return source.Descendants().Where(f => (string)f.Attribute("class") == className);
        }

    }
}
