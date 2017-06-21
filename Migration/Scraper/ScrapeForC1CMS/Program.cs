using ScrapeForC1CMS.CustomProviders;
using ScrapeForC1CMS.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ScrapeForC1CMS
{
    class Program
    {

        static void Main(string[] args)
        {
            var localizedHomepages = new Dictionary<CultureInfo, Uri>();

            // put your URLs and content Culture here - current example is importing two languages
            localizedHomepages.Add(CultureInfo.GetCultureInfo("en-US"), new Uri("http://www.denmarkvac.cn/index.html"));
            localizedHomepages.Add(CultureInfo.GetCultureInfo("zh-CN"), new Uri("http://www.denmarkvac.cn/chinese/index.html"));

            // write path to a temp c1 site here - will copy data/media to this, for immediate test.
            string pathToTestWebsite = @"C:\Users\marcus.wendt\Documents\My Web Sites\CompositeC19";

            // declare your providers here - the sample ones will probably not work out of the box, so next step if to make your own
            IContentParser contentParser = new CustomProviders.Samples.ContentParser();
            ITemplateChooser templateChooser = new CustomProviders.Samples.TemplateChooser();

            // and off we go ...
            var scraper = new WebsiteScraper(contentParser);
            var scrapeResult = scraper.Scrape(localizedHomepages);
            var rewriter = new UriRewriter(scrapeResult);
            rewriter.MakePathsInternal();
            DataSerializer serializer = new DataSerializer( templateChooser);
            serializer.WriteToXmlFiles(scrapeResult);

            CopyToTestWebsite(pathToTestWebsite);

            Console.Write("All Done...");
        }


        static void CopyToTestWebsite(string c1SiteRootPath)
        {
            CopyFiles(
                new DirectoryInfo( Utils.GetSubPath("Data")),
                new DirectoryInfo(Path.Combine(c1SiteRootPath, @"App_Data\Composite\DataStores")),
                true,
                "*.xml"
                );

            CopyFiles(
                new DirectoryInfo(Utils.GetSubPath("Media")),
                new DirectoryInfo(Path.Combine(c1SiteRootPath, @"App_Data\Media")),
                true,
                "*"
                );
        }

        static void CopyFiles(DirectoryInfo source,
                              DirectoryInfo destination,
                              bool overwrite,
                              string searchPattern)
        {
            FileInfo[] files = source.GetFiles(searchPattern);

            //this section is what's really important for your application.
            foreach (FileInfo file in files)
            {
                file.CopyTo(destination.FullName + "\\" + file.Name, overwrite);
            }
        }
    }
}
