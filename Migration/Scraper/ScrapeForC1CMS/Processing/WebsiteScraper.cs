using ScrapeForC1CMS.Data;
using ScrapeForC1CMS.CustomProviders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Xml;

namespace ScrapeForC1CMS.Processing
{
    public class WebsiteScraper
    {
        private IContentParser _contentParser;
        private Dictionary<Uri, XDocument> documentCache = new Dictionary<Uri, XDocument>();
        private PageTreeNode _topPageTreeNode = null;
        private XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
        private List<string> _validHosts = new List<string>();
        private Dictionary<Uri, string> _downloadCache = new Dictionary<Uri, string>();
        private Dictionary<Uri, string> _mimeCache = new Dictionary<Uri, string>();
        private List<Uri> _visited = new List<Uri>();


        public WebsiteScraper(IContentParser contentParser)
        {
            _contentParser = contentParser;
        }

        public SiteScrape Scrape(Dictionary<CultureInfo, Uri> localizedHomepages)
        {
            _validHosts.AddRange(localizedHomepages.Values.Select(f => f.Host).Distinct());

            // sanity checking we get the same ID from multiple homepages
            if (localizedHomepages.Select(f => GetPageIdFromUri(f.Value)).Distinct().Count() != 1) throw new InvalidOperationException("Getting different Page ID values from the analyzer, when feeding homepage URLs");

            _topPageTreeNode = new PageTreeNode { Id = GetPageIdFromUri(localizedHomepages.First().Value), Depth = 0 };

            foreach (var culture in localizedHomepages.Keys)
            {
                ResolveNodeStructured(localizedHomepages[culture], culture, _topPageTreeNode, "homepage");
                ResolveNodeRest(localizedHomepages[culture], culture, _topPageTreeNode);
            }

            SiteScrape result = new SiteScrape { Homepage = _topPageTreeNode, Files = new Files { CachedFiles = _downloadCache } };

            return result;
        }

        private void ResolveNodeStructured(Uri uri, CultureInfo culture, PageTreeNode pageTreeNode, string menuTitle)
        {
            if (!_validHosts.Contains(uri.Host)) return;

            if (pageTreeNode.PagesLocalized.ContainsKey(culture)) return;

            var pageContent = GetPageContent(uri, culture, menuTitle);
            pageTreeNode.PagesLocalized.Add(culture, pageContent);

            var linksSection = _contentParser.GetStructuredNavigationElements(uri, DocCache(uri)).Where(f=>f!=null);
            var linkElements = linksSection.Descendants().Where(e => e.Name == xhtmlNs + "a" && e.Attribute("href") != null);
            foreach (var aElement in linkElements)
            {
                Uri link = new Uri(uri, aElement.Attribute("href").Value);
                if (IsHtml(link))
                {
                    var referencedPageNode = GetNodeByUri(link);
                    if (referencedPageNode == null)
                    {
                        referencedPageNode = new PageTreeNode { Id = GetPageIdFromUri(link), Depth = pageTreeNode.Depth + 1 };
                        pageTreeNode.ChildNodes.Add(referencedPageNode);
                    }
                }
            }
            foreach (var aElement in linkElements)
            {
                Uri link = new Uri(uri, aElement.Attribute("href").Value);
                if (IsHtml(link))
                {
                    string lineMenuTitle = aElement.Value.Trim();
                    var referencedPageNode = GetNodeByUri(link);
                    ResolveNodeStructured(link, culture, referencedPageNode, lineMenuTitle);
                }
            }
        }

        private void ResolveNodeRest(Uri uri, CultureInfo culture, PageTreeNode pageTreeNode)
        {
            if (!_validHosts.Contains(uri.Host)) return;

            var linkElements = DocCache(uri).Descendants().Where(e => e.Name == xhtmlNs + "a" && e.Attribute("href") != null);

            var linkAttributes = DocCache(uri).Descendants().Attributes().Where(f => f.Name == "href" || f.Name == "src");

            foreach (var linkAttribute in linkAttributes)
            {
                Uri link = new Uri(uri, linkAttribute.Value);
                if (_validHosts.Contains(link.Host) && !_visited.Contains(link))
                {
                    _visited.Add(link);

                    if (IsHtml(link))
                    {
                        var referencedPageNode = GetNodeByUri(link);
                        if (referencedPageNode == null)
                        {
                            referencedPageNode = new PageTreeNode { Id = GetPageIdFromUri(link), Depth = pageTreeNode.Depth + 1 };
                            pageTreeNode.ChildNodes.Add(referencedPageNode);
                            ResolveNodeStructured(link, culture, referencedPageNode, linkAttribute.Parent.Value);
                        }
                        ResolveNodeRest(link, culture, referencedPageNode);
                    }
                }
            }
        }


        private PageTreeNode GetNodeByUri(Uri uri)
        {
            Guid pageId = GetPageIdFromUri(uri);

            PageTreeNode match = GetNodeById(pageId, _topPageTreeNode);

            return match;
        }

        private PageTreeNode GetNodeById(Guid pageId, PageTreeNode nodeToCheck)
        {
            if (nodeToCheck != null)
            {
                if (nodeToCheck.Id == pageId) return nodeToCheck;
                foreach (var child in nodeToCheck.ChildNodes)
                {
                    var nodeMatch = GetNodeById(pageId, child);
                    if (nodeMatch != null) return nodeMatch;
                }
            }

            return null;
        }

        private Guid GetPageIdFromUri(Uri uri)
        {
            return _contentParser.GetCultureInvariantPageId(uri, DocCache(uri));
        }

        private PageContent GetPageContent(Uri uri, CultureInfo culture, string menuTitle)
        {
            var pageContent = new PageContent();
            pageContent.Culture = culture;
            pageContent.MenuTitle = _contentParser.GetMenuTitle(uri, DocCache(uri), menuTitle);
            pageContent.SourceUri = uri;

            pageContent.Description = _contentParser.GetPageDescription(uri, DocCache(uri)) ?? "";
            pageContent.Title = _contentParser.GetPageTitle(uri, DocCache(uri)).Trim() ?? Path.GetFileNameWithoutExtension(uri.AbsolutePath);

            var pageUrlTitleSuggestion = (string.IsNullOrWhiteSpace(menuTitle) ? Path.GetFileNameWithoutExtension(uri.AbsolutePath) : menuTitle).Replace(" ", "-").Replace("?", "").Replace("&", "").Replace("#", "");
            pageContent.UrlTitle = _contentParser.GetPageUrlTitle(uri, DocCache(uri), pageUrlTitleSuggestion).Trim() ?? Path.GetRandomFileName();

            pageContent.PlaceholderContent = _contentParser.GetPlaceholderContent(uri, DocCache(uri)) ?? new Dictionary<string, List<XNode>>();

            return pageContent;
        }

        private bool IsHtml(Uri uri)
        {
            try
            {
                EnsureCache(uri);

                return _mimeCache[uri].StartsWith("text/html");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
      }


        private void EnsureCache(Uri uri)
        {
            if (!_mimeCache.ContainsKey(uri))
            {
                Cache(uri);
            }
        }

        private void Cache(Uri uri)
        {
            Console.WriteLine(uri);

            string localFileName = GetLocalPath(uri);

            if (File.Exists(localFileName))
            {
                var extension = Path.GetExtension(localFileName);
                var mime = Utils.GetMimeType(extension);
                if (mime!="application/octet-stream")
                {
                    _mimeCache.Add(uri, Utils.GetMimeType(extension));
                    _downloadCache.Add(uri, localFileName);
                    return;
                }
            }

            var request = HttpWebRequest.Create(uri) as HttpWebRequest;

            if (request != null)
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response != null)
                    {
                        if (!File.Exists(localFileName))
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(localFileName))) Directory.CreateDirectory(Path.GetDirectoryName(localFileName));

                            using (Stream output = File.OpenWrite(localFileName))
                            {
                                using (Stream input = response.GetResponseStream())
                                {
                                    input.CopyTo(output);
                                }
                            }
                        }

                        _mimeCache.Add(uri, response.ContentType);
                        _downloadCache.Add(uri, localFileName);
                    }
                    Console.WriteLine("done");
                }
            }

        }

        private string GetLocalPath(Uri uri)
        {
            var extension = Path.GetExtension(uri.LocalPath);

            var uriPath = uri.PathAndQuery.Substring(1);
            if (string.IsNullOrEmpty(uriPath)) uriPath = "index.html";
            if (extension.Length > 0)
            {
                uriPath = uriPath.Replace(extension, "");
            }
            uriPath = uriPath.Replace("?","-").Replace("&", "-").Replace("=", "-") + extension;
            string localFileName = Utils.GetSubPath("cache/" + uriPath);
            if (localFileName.Length > 248) localFileName = localFileName.Substring(0, 248);

            localFileName = localFileName.Replace("/", @"\");
            if (localFileName.EndsWith(@"\"))
            {
                if (localFileName.Contains("json"))
                {
                    // the !json part is wp specific
                    localFileName = localFileName + "index.json";

                }
                else
                {
                    localFileName = localFileName + "index.html";
                }
            }
            else
            {
                if (extension == "")
                {
                    localFileName = localFileName + ".html";
                }
            }
            return localFileName;
        }

        private XDocument DocCache(Uri uri)
        {
            EnsureCache(uri);
            // for messy html sites, adding a TidyHTML task here would make a lot of sense
            if (!documentCache.ContainsKey(uri))
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument htmlDoc = web.Load(GetLocalPath(uri));
                htmlDoc.OptionOutputAsXml = true;
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlTextWriter xw = new System.Xml.XmlTextWriter(sw))
                    {
                        htmlDoc.Save(xw);
                    }
                    string html = sw.ToString();
                    if (!html.Contains(xhtmlNs.ToString()))
                    {
                        html = html.Replace("<html", $"<html xmlns='{xhtmlNs}'");
                    }
                    
                    XDocument doc = XDocument.Parse(html);
                    documentCache.Add(uri, doc);
                }
            }

            return documentCache[uri];
        }
    }
}
