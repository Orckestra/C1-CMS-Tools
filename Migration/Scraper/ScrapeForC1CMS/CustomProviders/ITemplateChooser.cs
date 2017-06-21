using ScrapeForC1CMS.Data;
using System;
using System.Globalization;

namespace ScrapeForC1CMS.CustomProviders
{
    public interface ITemplateChooser
    {
        /// <summary>
        /// Given a PageTreeNode return a Page Type ID
        /// </summary>
        /// <param name="pageTreeNode">The page node (giving access to structure location and content)</param>
        /// <returns>A page type id</returns>
        Guid GetPageTypeId(PageTreeNode pageTreeNode);

        /// <summary>
        /// Given a PageTreeNode (and the culture of the concrete page this will be used on) return a Page Template ID.
        /// 
        /// For very different cultures it might make sense to return different templates, but it is encouraged to have culture invariant templates.
        /// </summary>
        /// <param name="pageTreeNode">The page node (giving access to structure location and content)</param>
        /// <param name="culture">The culture this will be used on</param>
        /// <returns>A page template id</returns>
        Guid GetPageTemplateId(PageTreeNode pageTreeNode, CultureInfo culture);
    }
}
