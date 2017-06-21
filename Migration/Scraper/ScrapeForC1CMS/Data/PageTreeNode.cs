using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapeForC1CMS.Data
{
    public class PageTreeNode
    {
        public PageTreeNode()
        {
            this.PagesLocalized = new Dictionary<CultureInfo, PageContent>();
            this.ChildNodes = new List<PageTreeNode>();
        }

        public Guid Id { get; set; }
        public Dictionary<CultureInfo, PageContent> PagesLocalized { get; set; }
        public List<PageTreeNode> ChildNodes { get; set; }
        public int Depth { get; set; }

        public override string ToString()
        {
            if (PagesLocalized.Any())
            {
                return PagesLocalized.First().Value.Title;
            }
            return base.ToString();
        }
    }
}
