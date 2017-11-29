## Synopsis

The CacheService is a utility that can be used to fetch built-in Sitefinity types and cache them as they are used.
A programmer is required for both the installation of the service and also to create the concrete classes that will be cached after the Sitefinity data is retrieved from the database.

Once the service is installed and concrete types are created. A programmer only needs to pass through an IQueryable to the service for fetch objects. The service will then:
1. Determine if the objects that are being fetched are already cached.
2. If the objects are already cached, they will be returned directly from the cache.
3. If the objects are not cached, the query will be executed, results cached, results returned.

Various caching options are available.
- Cache all results as a list
- Cache all results as a dictionary
- Cache all reuslts as a linked dictionary (keys can be shared but items linked to one-another)

## Code Example

Example use of the cache service. 
Retrieving items from the cache as a linked dictionary:
```
public Dictionary<string, EventModel> GetLiveEvents()
{
    //Initialize dictionary for housing events
    Dictionary<string, EventModel> events = new Dictionary<string, EventModel>();

    //Get the EventsManager for building an EventQuery
    var moduleManager = EventsManager.GetManager();

    //Build the query for returning all live events
    IQueryable<Event> resultsQuery =
                        moduleManager.GetEvents()
                            .Where(p => p.Status == ContentLifecycleStatus.Live)
                            .OrderBy(p => p.EventStart);

    //Pass the query along with the cache key and the two types we are working with.
    //If cached items are returned, we will use those. If nothing is returned we will execute the query and store
    //those to the cache
    events = _cacheService.GetLinkedDictionaryItems<EventModel, Event>("Event-Dictionary", resultsQuery);

    return events;
}
```
Retrieving items from the cache as a list:
```
public List<EventModel> GetLiveEvents()
{
    //Initialize dictionary for housing events
    List<EventModel> events = new List<EventModel>();

    //Get the EventsManager for building an EventQuery
    var moduleManager = EventsManager.GetManager();

    //Build the query for returning all live events
    IQueryable<Event> resultsQuery =
                        moduleManager.GetEvents()
                            .Where(p => p.Status == ContentLifecycleStatus.Live)
                            .OrderBy(p => p.EventStart);

    //Pass the query along with the cache key and the two types we are working with.
    //If cached items are returned, we will use those. If nothing is returned we will execute the query and store
    //those to the cache
    events = _cacheService.GetListItems<EventModel, Event>("Event-List", resultsQuery);

    return events;
}
```
Example model that implements cache interface:
```

/// <summary>
/// ICacheDataLinkable EventModel, allows individual events to link to next scheduled for the day. Allows for quicker dictionary lookup.
/// ICacheDataLinkable is a generic taking in the model and the sitefinity data type that is mapped to it
/// </summary>
public class EventModel : ICacheDataLinkable<EventModel, Event>
{
    #region Private Fields

    //EventModel node linking to next event
    private EventModel _next;

    #endregion

    #region Public Properties

    public string EventName { get; set; }
    public string UrlName { get; set; }
    public string Url { get; set; }
    public DateTime PublishedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Type TheType { get; set; }
    public Guid Id { get; set; }

    /// <summary>
    /// Property that access the next linked event
    /// </summary>
    public EventModel Next
    {
        get
        {
            return _next;
        }

        set
        {
            _next = value;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets EventModel Url using Sitefinity's Content Location Service
    /// </summary>
    /// <returns></returns>
    private string GetUrl()
    {
        var clService = SystemManager.GetContentLocationService();

        var location = clService.GetItemDefaultLocation(
            TheType,
            null,
            Id);

        if (location != null)
            return location.ItemAbsoluteUrl;

        return string.Empty;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the current object type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Type GetContentType<T>()
    {
        return typeof(T);
    }

    /// <summary>
    /// Sets all necessary values from the mapped Sitefinity content type
    /// </summary>
    /// <param name="sitefinityContent"></param>
    public void ProcessItem(Event sitefinityContent)
    {
        Id = sitefinityContent.Id;
        UrlName = sitefinityContent.UrlName;
        PublishedDate = sitefinityContent.PublicationDate;
        EventName = sitefinityContent.Title;
        TheType = sitefinityContent.GetType();
        StartDate = sitefinityContent.EventStart;
        EndDate = sitefinityContent.EventEnd;
        Url = GetUrl();
    }

    /// <summary>
    /// Default Sitefinity content data query
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IQueryable<T> GetBaseSitefinityDataSetQuery<T>()
    {
        EventsManager eventsMgr = EventsManager.GetManager();
        return eventsMgr.GetEvents().Where(e => e.Status == ContentLifecycleStatus.Live) as IQueryable<T>;
    }

    /// <summary>
    /// Item key that is used to find a given object in the cached dictionary
    /// </summary>
    /// <returns></returns>
    public string ItemKey()
    {
        return StartDate.Value.Month.ToString() + "m" + StartDate.Value.Day;
    }

    #endregion

    #region Constructors

    public EventModel()
    {
        _next = null;
    }

    #endregion
}
```

The cache service requires that the eventmodel implement either the ICacheData or ICacheDataLinkable interfaces.

## Motivation

This service was built in response to various and frequent Sitefinity projects requiring a faster look up time for data queries and results. 
Sitefinity only provides options for caching content on the page level (when the page is rendered to the user) through the page output cache.  
This cache service gives the programmer flexibility to use Sitefinity's CacheManager for caching queried results for data that is used frequently in many places or high traffic pages.

## Installation

Installation requires pulling down this repository, zip, or dll and referencing it by a support Sitefinity version.

## Supported Versions

CMS.Sitefinity.CacheService v9.0.6010.0
- Built on Sitefinity v9.0.6010.0
