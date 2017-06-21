using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScrapeForC1CMS.Data;

namespace ScrapeForC1CMS.CustomProviders.Samples
{
    class TemplateChooser : ITemplateChooser
    {
        public Guid GetPageTemplateId(PageTreeNode pageTreeNode, CultureInfo culture)
        {
            if (pageTreeNode.Depth == 0)
            {
                return new Guid("a270f819-0b5c-4f7e-9194-4b554043e4ab"); // Venus: Front page
            }
            if (pageTreeNode.Depth == 1 && !pageTreeNode.ChildNodes.Any())
            {
                if (pageTreeNode.PagesLocalized[culture].PlaceholderContent.ContainsKey("aside"))
                {
                    return new Guid("9f096519-d21c-435e-b334-62224fde2ab3"); // Venus: Page with right aside (no navigation)
                }
                return new Guid("0526ad34-c540-418e-8c23-0eec2a8da2ce"); // Venus: Page (no aside or left navigation)
            }

            if (pageTreeNode.PagesLocalized.ContainsKey(culture))
            {
                if (pageTreeNode.PagesLocalized[culture].PlaceholderContent.ContainsKey("aside")) return new Guid("53851f7a-3f4b-4eda-9708-0743b6020e68"); // Venus: Page with navigation and right aside
            }

            return new Guid("e3851f7a-3f4b-4eda-9708-07c3b6020e08"); // Venus: Page with navigation
        }

        public Guid GetPageTypeId(PageTreeNode pageTreeNode)
        {
            if (pageTreeNode.Depth == 0)
            {
                return new Guid("de22fed1-0729-4ad3-aa1c-6047e54bf429"); // "Home" page type
            }

            return new Guid("f7869eb2-7369-4eb2-af47-e3be261e92c7"); // "Page" page type
        }
    }
}
