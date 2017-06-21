using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapeForC1CMS.Data
{
    public class Files
    {
        public Files()
        {
            CachedFiles = new Dictionary<Uri, string>();
            UsedMedia = new Dictionary<Uri, Guid>();
        }
        public Dictionary<Uri, string> CachedFiles { get; set; }
        public Dictionary<Uri, Guid> UsedMedia { get; set; }
    }
}
