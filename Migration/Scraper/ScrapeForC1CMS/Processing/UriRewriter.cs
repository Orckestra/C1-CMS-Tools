using ScrapeForC1CMS.Data;
using System;
using System.Linq;
using System.Xml.Linq;

namespace ScrapeForC1CMS.Processing
{
    public class UriRewriter
    {
        private PageTreeNode _rootPageTreeNode;
        Files _files;

        public UriRewriter(SiteScrape scrape)
        {
            _rootPageTreeNode = scrape.Homepage;
            _files = scrape.Files;
        }


        public void MakePathsInternal()
        {
            MakePathsInternal(_rootPageTreeNode);
        }

        private void MakePathsInternal(PageTreeNode pageTreeNode)
        {
            if (_rootPageTreeNode == null) _rootPageTreeNode = pageTreeNode;

            foreach (var localizedPage in pageTreeNode.PagesLocalized)
            {
                Uri pageUri = localizedPage.Value.SourceUri;

                foreach (var placeholderNodes in localizedPage.Value.PlaceholderContent.Values)
                {
                    foreach(XNode contentNode in placeholderNodes)
                    {
                        if (contentNode is XElement)
                        {
                            var contentElement = (XElement)contentNode;
                            var referenceAttributes = contentElement.DescendantsAndSelf().Attributes("href").Concat(contentElement.DescendantsAndSelf().Attributes("src"));

                            foreach (var referenceAttribute in referenceAttributes)
                            {
                                var fullUri = new Uri(pageUri, (string)referenceAttribute);
                                var internalPath = GetInternalPathByImportUri(fullUri);
                                if (internalPath!=null)
                                {
                                    referenceAttribute.Value = internalPath;
                                }

                            }
                        }
                    }

                }
            }

            foreach (var item in pageTreeNode.ChildNodes)
            {
                MakePathsInternal(item);
            }
        }

        private string GetInternalPathByImportUri( Uri sourceUri)
        {
            var found = GetInternalPathByImportUri(sourceUri, _rootPageTreeNode);

            if (found==null && _files.CachedFiles.ContainsKey(sourceUri))
            {
                Guid mediaId = Utils.GetGuidFromstring(_files.CachedFiles[sourceUri]);
                if (!_files.UsedMedia.ContainsKey(sourceUri))
                {
                    _files.UsedMedia.Add(sourceUri, mediaId);
                }
                return $"~/media({mediaId})";
            }

            return found;
        }

        private string GetInternalPathByImportUri(Uri sourceUri, PageTreeNode pageTreeNode)
        {
            if (pageTreeNode.PagesLocalized.Any( f=> f.Value.SourceUri == sourceUri))
            {
                return $"~/page({pageTreeNode.Id})";
            }

            foreach (var child in pageTreeNode.ChildNodes)
            {
                var internalPath = GetInternalPathByImportUri(sourceUri, child);
                if (internalPath!= null)
                {
                    return internalPath;
                }
            }

            return null;
        }

    }
}
