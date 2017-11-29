using CMS.Sitefinity.CacheService.Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Sitefinity.CacheService.Services.Abstract
{
    /// <summary>
    /// Interface for implementing a data service that caches Sitefinity content items
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Retrieves base content items from cache that have been stored as a list
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="cacheOnFail">Cache from database if cache returns null</param>
        /// <returns>list of cached items</returns>
        List<T> GetBaseListItems<T, U>(bool cacheOnFail = true, int expirationMinutes = 0) where T : ICacheData<U>;

        /// <summary>
        /// Retrieves base content items from cache that have been stored as a dictionary
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="cacheOnFail">Cache from database if cache returns null</param>
        /// <returns>dictionary of cached items</returns>
        Dictionary<string, T> GetBaseDictionaryItems<T, U>(bool cacheOnFail = true, int expirationMinutes = 0) where T : ICacheData<U>;

        /// <summary>
        /// Retrieves content items from cache of a specified key that have been stored as a list
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">cache key</param>
        /// <param name="failQuery">Query that runs if returned cached items is null</param>
        /// <returns>list of cached items</returns>
        List<T> GetListItems<T, U>(string key, IQueryable<U> failQuery = null, int expirationMinutes = 0) where T : ICacheData<U>;

        /// <summary>
        /// Retrieves content items from cache of a specified key that have been stored as a dictionary
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">cache key</param>
        /// <param name="failQuery">Query that runs if returned cached items is null</param>
        /// <returns>dictionary of cached items</returns>
        Dictionary<string, T> GetDictionaryItems<T, U>(string key, IQueryable<U> failQuery = null, int expirationMinutes = 0) where T : ICacheData<U>;

        /// <summary>
        /// Retrieves content items from cache of a specified key that have been stored as a dictionary of linked lists
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">Cache Key</param>
        /// <param name="failQuery">Query that runs if returned cached items is null</param>
        /// <param name="expirationMinutes"></param>
        /// <returns></returns>
        Dictionary<string, T> GetLinkedDictionaryItems<T, U>(string key, IQueryable<U> failQuery = null, int expirationMinutes = 0) where T : ICacheDataLinkable<T, U>;

        /// <summary>
        /// Cache data set as a dictionary of linked lists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="sfContentQuery"></param>
        /// <param name="key"></param>
        /// <param name="expirationMinutes"></param>
        void CacheDataSetAsLinkedDictionary<T, U>(IQueryable<U> sfContentQuery, string key, int expirationMinutes = 0) where T : ICacheDataLinkable<T, U>;

        /// <summary>
        /// Caches base data set, implemented in cacheable, as dictionary
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        void CacheBaseDataSetAsList<T, U>(int expirationMinutes = 0) where T : ICacheData<U>;

        /// <summary>
        /// Caches base data set, implemented in cacheable, as dictionary
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        void CacheBaseDataSetAsDictionary<T, U>(int expirationMinutes = 0) where T : ICacheData<U>;

        /// <summary>
        /// Caches provided data set as a list from IQueryable by specified key
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="sfContentQuery">Sitefintiy data set to be cached</param>
        /// <param name="key">Key associated with set in cache</param>
        void CacheDataSetAsList<T, U>(IQueryable<U> sfContentQuery, string key, int expirationMinutes = 0) where T : ICacheData<U>;

        /// <summary>
        /// Caches provided data set as a dictionary from IQueryable by specified key
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="sfContentQuery">Sitefintiy data set to be cached</param>
        /// <param name="key">Key associated with set in cache</param>
        void CacheDataSetAsDictionary<T, U>(IQueryable<U> sfContentQuery, string key, int expirationMinutes = 0) where T : ICacheData<U>;

        /// <summary>
        /// Clears all cached list items from the cache via provided key
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">key to clear</param>
        void ClearCachedListItems<T, U>(string key) where T : ICacheData<U>;

        /// <summary>
        /// Clears all cached dictionary items from the cache via provided key
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">optional cache key to clear</param>
        void ClearCachedDictionaryItems<T, U>(string key) where T : ICacheData<U>;

        /// <summary>
        /// Clears all base list items from the cache
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        void ClearCachedListItems<T, U>() where T : ICacheData<U>;

        /// <summary>
        /// Clears all base dictionary items from the cache
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        void ClearCachedDictionaryItems<T, U>() where T : ICacheData<U>;

        /// <summary>
        /// Adds individual cacheable object to the cache
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="dataItem">data item to cache</param>
        void AddToCache<T, U>(T dataItem) where T : ICacheData<U>;

        /// <summary>
        /// Caches a single object
        /// </summary>
        /// <typeparam name="T">Type of object being cached</typeparam>
        /// <param name="dataItem">Object being cached</param>
        /// <param name="key">Key for cached object</param>
        void CacheObject<T>(T dataItem, string key);

        /// <summary>
        /// Returns a single cache object from provided key
        /// </summary>
        /// <typeparam name="T">Type of object being returned</typeparam>
        /// <param name="key">Key for looking up cached object</param>
        /// <returns>Object of type T</returns>
        T GetObject<T>(string key);

        /// <summary>
        /// Clears the cache for a provided cache key
        /// </summary>
        /// <param name="key">Cache key</param>
        void ClearCachedObject(string key);
    }
}
