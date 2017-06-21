using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ScrapeForC1CMS.Data
{
    public class PageContent
    {
        public PageContent()
        {
            this.PlaceholderContent = new Dictionary<string, List<XNode>>();
        }
        public Uri SourceUri { get; set; }
        public CultureInfo Culture { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MenuTitle { get; set; }
        public string UrlTitle { get; set; }
        public Dictionary<string,List<XNode>> PlaceholderContent { get; set; }

    }
}
