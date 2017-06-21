using ScrapeForC1CMS.CustomProviders;
using ScrapeForC1CMS.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ScrapeForC1CMS.Processing
{
    public class DataSerializer
    {
        private XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
        private ITemplateChooser _templateChooser;

        public DataSerializer(ITemplateChooser templateChooser)
        {
            _templateChooser = templateChooser;
        }

        public void WriteToXmlFiles(SiteScrape siteScrape)
        {
            PageTreeNode pageTreeNode = siteScrape.Homepage;

            var pageStructureElementsElements = new XElement("PageStructureElementsElements");
            pageStructureElementsElements.Add(GetPageStructureElements(pageTreeNode, Guid.Empty, 0));
            Save(pageStructureElementsElements, "Composite.Data.Types.IPageStructure.xml");

            foreach (var culture in pageTreeNode.PagesLocalized.Keys)
            {
                var pageElementsElements = new XElement("PageElementsElements");
                pageElementsElements.Add(GetPageElements(pageTreeNode, culture));
                Save(pageElementsElements, $"Composite.Data.Types.IPage_{culture}.xml");
                Save(pageElementsElements, $"Composite.Data.Types.IPage_Unpublished_{culture}.xml");

                var pagePlaceholderContentElementsElements = new XElement("PagePlaceholderContentElementsElements");
                pagePlaceholderContentElementsElements.Add(GetPagePlaceholderContentElements(pageTreeNode, culture));
                Save(pagePlaceholderContentElementsElements, $"Composite.Data.Types.IPagePlaceholderContent_{culture}.xml");
                Save(pagePlaceholderContentElementsElements, $"Composite.Data.Types.IPagePlaceholderContent_Unpublished_{culture}.xml");
            }


            var mediaFileDataElementsElements = new XElement("MediaFileDataElementsElements");
            mediaFileDataElementsElements.Add(GetMediaFileDataElements(siteScrape.Files));
            Save(mediaFileDataElementsElements, "Composite.Data.Types.IMediaFileData.xml");


            var mediaFolderDataElementsElements = new XElement("MediaFolderDataElementsElements");
            var folders = mediaFileDataElementsElements.Elements().Attributes("FolderPath").Select(f => f.Value).Distinct();
            mediaFolderDataElementsElements.Add(GetMediaFolderDataElements(folders));
            Save(mediaFolderDataElementsElements, "Composite.Data.Types.IMediaFolderData.xml");

        }

        private IEnumerable<XElement> GetMediaFolderDataElements(IEnumerable<string> folders)
        {
            List<string> allFolders = new List<string>();
            allFolders.AddRange(folders);

            foreach (var folder in folders)
            {
                if (!folder.StartsWith("/")) throw new InvalidOperationException("Getting folders not beginning with / - not supported");

                var segments = folder.Split('/');
                string segmentPath = "";
                for (int i = 1; i < segments.Length; i++)
                {
                    segmentPath = segmentPath + "/" + segments[i];
                    if (!allFolders.Contains(segmentPath))
                    {
                        allFolders.Add(segmentPath);
                    }
                }
            }
            foreach (var folder in allFolders.Distinct().OrderBy(f => f))
            {
                yield return new XElement("MediaFileDataElements",
                    new XAttribute("Id", Guid.NewGuid()),
                    new XAttribute("Path", folder));
            }
        }

        private IEnumerable<XElement> GetMediaFileDataElements(Files files)
        {
            string mediaDir = Utils.GetSubPath("Media");
            if (!Directory.Exists(mediaDir))
            {
                Directory.CreateDirectory(mediaDir);
            }

            foreach (var mediaFile in files.UsedMedia.Where(f => f.Key.LocalPath.Length > 1))
            {
                string cachedFilePath = files.CachedFiles[mediaFile.Key];
                string originalFileName = Path.GetFileName(cachedFilePath).Replace("%20", " ");
                string extension = Path.GetExtension(cachedFilePath);
                string fileName = $"{mediaFile.Value}";
                File.Copy(cachedFilePath, Path.Combine(mediaDir, fileName), true);

                FileInfo fileInfo = new FileInfo(cachedFilePath);

                yield return new XElement("MediaFileDataElements",
                    new XAttribute("Id", mediaFile.Value),
                    new XAttribute("FolderPath", Path.GetDirectoryName(mediaFile.Key.LocalPath).Replace("\\", "/")),
                    new XAttribute("FileName", originalFileName),
                    new XAttribute("Title", originalFileName),
                    new XAttribute("Description", "*** No data really? ***"),
                    new XAttribute("CultureInfo", "en-US"),
                    new XAttribute("MimeType", Utils.GetMimeType(extension)),
                    new XAttribute("Length", fileInfo.Length),
                    new XAttribute("CreationTime", DateTime.Now),
                    new XAttribute("LastWriteTime", DateTime.Now)
                    );
            }
        }

        private IEnumerable<XElement> GetPageStructureElements(PageTreeNode pageTreeNode, Guid parentId, int position)
        {
            yield return new XElement("PageStructureElements",
                new XAttribute("Id", pageTreeNode.Id),
                new XAttribute("ParentId", parentId),
                new XAttribute("LocalOrdering", position)
                );

            int childCounter = 0;
            foreach (var child in pageTreeNode.ChildNodes)
            {
                var subTree = GetPageStructureElements(child, pageTreeNode.Id, childCounter++);
                foreach (var item in subTree)
                {
                    yield return item;
                }
            }
        }

        private IEnumerable<XElement> GetPageElements(PageTreeNode pageTreeNode, CultureInfo culture)
        {
            if (!pageTreeNode.PagesLocalized.ContainsKey(culture)) yield break;

            var localizedPageContent = pageTreeNode.PagesLocalized[culture];

            yield return new XElement("PageElements",
                new XAttribute("PublicationStatus", "published"),
                new XAttribute("ChangeDate", DateTime.Now),
                new XAttribute("CreationDate", DateTime.Now),
                new XAttribute("ChangedBy", "import"),
                new XAttribute("CreatedBy", "import"),
                new XAttribute("Id", pageTreeNode.Id),
                new XAttribute("TemplateId", _templateChooser.GetPageTemplateId(pageTreeNode, culture)),
                new XAttribute("PageTypeId", _templateChooser.GetPageTypeId(pageTreeNode)),
                new XAttribute("Title", localizedPageContent.Title),
                new XAttribute("MenuTitle", localizedPageContent.MenuTitle),
                new XAttribute("UrlTitle", localizedPageContent.UrlTitle),
                new XAttribute("FriendlyUrl", ""),
                new XAttribute("Description", localizedPageContent.Description),
                new XAttribute("SourceCultureName", culture),
                new XAttribute("VersionId", pageTreeNode.Id)
                );

            foreach (var child in pageTreeNode.ChildNodes)
            {
                var subTree = GetPageElements(child, culture);
                foreach (var item in subTree)
                {
                    yield return item;
                }
            }
        }

        private IEnumerable<XElement> GetPagePlaceholderContentElements(PageTreeNode pageTreeNode, CultureInfo culture)
        {
            if (!pageTreeNode.PagesLocalized.ContainsKey(culture)) yield break;

            var localizedPageContent = pageTreeNode.PagesLocalized[culture];

            foreach (var item in localizedPageContent.PlaceholderContent)
            {
                yield return new XElement("PagePlaceholderContentElements",
                    new XAttribute("PublicationStatus", "published"),
                    new XAttribute("ChangeDate", DateTime.Now),
                    new XAttribute("CreationDate", DateTime.Now),
                    new XAttribute("ChangedBy", "import"),
                    new XAttribute("CreatedBy", "import"),
                    new XAttribute("PageId", pageTreeNode.Id),
                    new XAttribute("PlaceHolderId", item.Key),
                    new XAttribute("Content", GetXhtmlDocument(item.Value).ToString()),
                    new XAttribute("SourceCultureName", culture),
                    new XAttribute("VersionId", pageTreeNode.Id)
                    );
            }

            foreach (var child in pageTreeNode.ChildNodes)
            {
                var subTree = GetPagePlaceholderContentElements(child, culture);
                foreach (var item in subTree)
                {
                    yield return item;
                }
            }
        }


        private XElement GetXhtmlDocument(IEnumerable<XNode> bodyContent)
        {
            XElement html = new XElement(xhtmlNs + "html",
                new XElement(xhtmlNs + "head"),
                new XElement(xhtmlNs + "body", bodyContent));

            return html;
        }


        private void Save(XElement doc, string filename)
        {
            string dataDir = Utils.GetSubPath("Data");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            doc.Save(Path.Combine(dataDir, filename));
        }

    }
}