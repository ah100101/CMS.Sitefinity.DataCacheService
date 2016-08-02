using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Sitefinity.Configuration;

namespace SiteStack.Sitefinity.CacheService.Configuration
{
    public class CacheServiceConfiguration : ConfigSection
    {
        [ConfigurationProperty("CachingOn", DefaultValue = true)]
        public bool CachingOn
        {
            get
            {
                return (bool)this["CachingOn"];
            }

            set
            {
                this["CachingOn"] = value;
            }
        }
    }
}
