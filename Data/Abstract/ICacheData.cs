using System;
using System.Linq;

namespace CMS.Sitefinity.CacheService.Data.Abstract
{
    /// <summary>
    /// CacheableData interface for use with ICacheableDataService. CacheableDataService requires objects being cached to implement ICacheableData
    /// </summary>
    public interface ICacheData<T>
    {
        /// <summary>
        /// Unique identifier for the cacheable data item
        /// </summary>
        Guid Id
        {
            get;
        }

        /// <summary>
        /// The Sitefinity content type being processed
        /// </summary>
        Type GetContentType<T>();

        /// <summary>
        /// Sets necessary members from Sitefinity content item
        /// </summary>
        /// <param name="T">Sitefinity content type to be parsed</param>
        void ProcessItem(T sitefinityContent);

        /// <summary>
        /// Returns a unique string for looking up the item
        /// </summary>
        /// <returns></returns>
        string ItemKey();

        /// <summary>
        /// Gets IQueryable that returns what's deemed as the base data set of Sitefinity items to be cached
        /// </summary>
        /// <returns>IQueryable</returns>
        IQueryable<T> GetBaseSitefinityDataSetQuery<T>();
    }
}
