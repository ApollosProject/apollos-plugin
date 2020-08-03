using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData;

using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;
using Rock.Rest;
using Rock;
using System.Data.Entity;

namespace apollosproject.ApollosPlugin.Rest
{
    public class ApollosController : ApiControllerBase
    {


        #region GetContentByIds
        /// <summary>
        /// Returns a list of content channel items based on a list of content channel item ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/GetContentChannelItemsByIds")]
        public IQueryable<ContentChannelItem> GetByIds( string ids)
        {
            RockContext rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;
            List<int> idList = ids.Split(',').Select(int.Parse).ToList();
            IQueryable<ContentChannelItem> contentChannelItemList = new ContentChannelItemService(rockContext).Queryable().AsNoTracking().Where(item => idList.Contains(item.Id)) ;

            return contentChannelItemList;
        }
        #endregion

        #region GetInteractionsByForeignKeys
        /// <summary>
        /// Returns a list of interactions based on a list of foreign keys
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/GetInteractionsByForeignKeys")]
        public IQueryable<Interaction> GetInteractionsByForeignKeys(string keys)
        {
            RockContext rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;
            List<string> keyList = keys.Split(',').ToList();
            IQueryable<Interaction> interactionList = new InteractionService(rockContext).Queryable().AsNoTracking().Where(i => keyList.Contains(i.ForeignKey));

            return interactionList;
        }
        #endregion


        #region ContentByDataViewGuids
        /// <summary>
        /// Returns a list of content channel items based on a list of dataview guids
        /// </summary>
        /// <param name="guids"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/ContentChannelItemsByDataViewGuids")]
        public IQueryable<ContentChannelItem> GetFromPersonDataView(string guids)
        {
            var rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;

            // Turn the comma separated list of guids into a list of strings.
            List<string> guidList = (guids ?? "").Split(',').ToList();

            // Get the Id of the Rock.Model.ContentChannelItem Entity.
            int contentChannelItemEntityTypeId = EntityTypeCache.Get("Rock.Model.ContentChannelItem").Id;

            // Get the Field Type (Attribute Type) Id of the Data View Field Type.
            int fieldTypeId = FieldTypeCache.Get(Rock.SystemGuid.FieldType.DATAVIEWS.AsGuid()).Id;

            // Get the list of attributes that are of the Rock.Model.ContentChannelItem entity type
            // and that are of the Data View field type.
            List<int> attributeIdList = new AttributeService(rockContext)
                .GetByEntityTypeId(contentChannelItemEntityTypeId)
                .Where(item => item.FieldTypeId == fieldTypeId)
                .Select(a => a.Id)
                .ToList();



            // I want a list of content channel items whose ids match up to attribute values that represent entity ids
            IQueryable<ContentChannelItem> contentChannelItemList = new ContentChannelItemService(rockContext)
                .Queryable()
                .AsNoTracking()
                .WhereAttributeValue(rockContext, av => attributeIdList.Contains(av.AttributeId) && guidList.Any(guid => av.Value.Contains(guid)));



            // Return this list
            return contentChannelItemList;

        }

        #endregion


        #region DataViewByPerson
        /// <summary>
        /// Returns a list of dataviews that a person is a part of
        /// </summary>
        /// <param name="entityTypeId"></param>
        /// <param name="entityId"></param>
        /// <param name="categoryGuid"></param>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/GetPersistedDataViewsForEntity/{entityTypeId}/{entityId}")]
        public IQueryable<DataView> GetPersistedDataViewsForEntity(int entityTypeId, int entityId, System.Guid? categoryGuid = null, int categoryId = 0)
        {
            var rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;

            // Get the data view guids from the DataViewPersistedValues table that the Person Id is a part of
            var persistedValuesQuery = rockContext.DataViewPersistedValues.AsNoTracking().Where(a => a.EntityId == entityId && a.DataView.EntityTypeId == entityTypeId);
            IQueryable<DataView> dataViewList = persistedValuesQuery.Select(a => a.DataView);

            if (categoryGuid != null)
            {
                dataViewList = dataViewList.Where(a => a.Category.Guid == categoryGuid);
            }

            if (categoryId != 0)
            {
                dataViewList = dataViewList.Where(a => a.CategoryId == categoryId);
            }

            // Return DataView as IQueryable
            return dataViewList;

        }
        #endregion

        #region GetEventItemOccurencesByCalendarId
        /// <summary>
        /// Returns a list of event item occurences, filtered by a specific CalendarId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/GetEventItemOccurencesByCalendarId")]
        public IQueryable<EventItemOccurrence> GetEventItemOccurencesByCalendarId(int id)
        {
            var rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;
            EventItemOccurrenceService eventItemOccurenceService = new EventItemOccurrenceService(rockContext);

            IQueryable<EventItemOccurrence> itemOccurences = eventItemOccurenceService.Queryable().AsNoTracking().Join(new EventCalendarItemService(rockContext).Queryable(), occurence => occurence.EventItemId, eventCalendarItem => eventCalendarItem.EventItemId, (occurence, eventCalendarItem) => new
            {
                CalendarId = eventCalendarItem.EventCalendarId,
                EventItemOccurence = occurence
            }).Where(ec => ec.CalendarId == id).Select(ec => ec.EventItemOccurence);
            return itemOccurences;
        }
        #endregion


        #region ContentChannelItemsByCampusIdAndAttributeValue
        /// <summary>
        /// Returns a list of content channel items filtered by an attribute value and a campus id
        /// </summary>
        /// <param name="attributeValues"></param>
        /// <param name="attributeKey"></param>
        /// <param name="campusId"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/ContentChannelItemsByCampusIdAndAttributeValue")]
        public IQueryable<ContentChannelItem> ContentChannelItemsByCampusIdAndAttributeValue(string attributeValues, string attributeKey, int campusId)
        {
            var rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;

            // Turn the comma separated list of guids into a list of strings.
            List<string> attributeValueList = (attributeValues ?? "").Split(',').ToList();

            string campusGuid = new CampusService(rockContext).Get(campusId).Guid.ToString();

            // Get the Id of the Rock.Model.ContentChannelItem Entity.
            int contentChannelItemEntityTypeId = EntityTypeCache.Get("Rock.Model.ContentChannelItem").Id;

            // Get the Field Type (Attribute Type) Id of the Data View Field Type.
            int fieldTypeId = FieldTypeCache.Get(Rock.SystemGuid.FieldType.CAMPUSES.AsGuid()).Id;

            // Get the list of attributes that are of the Rock.Model.ContentChannelItem entity type
            // and that are of the Campus field type.
            List<int> campusAttributeIdList = new AttributeService(rockContext)
                .GetByEntityTypeId(contentChannelItemEntityTypeId)
                .Where(item => item.FieldTypeId == fieldTypeId)
                .Select(a => a.Id)
                .ToList();

            // Get the list of attributes that are of the Rock.Model.ContentChannelItem entity type
            // and that are named the the same as the incoming attributeName
            List<int> attributeIdList = new AttributeService(rockContext)
                .GetByEntityTypeId(contentChannelItemEntityTypeId)
                .Where(item => item.Key == attributeKey)
                .Select(a => a.Id)
                .ToList();



            // I want a list of content channel items whose ids match up to attribute values that represent entity ids
            IQueryable<ContentChannelItem> contentChannelItemList = new ContentChannelItemService(rockContext)
                .Queryable()
                .AsNoTracking()
                .WhereAttributeValue(rockContext, av => campusAttributeIdList.Contains(av.AttributeId) && av.Value.Contains(campusGuid))
                .WhereAttributeValue(rockContext, av => attributeIdList.Contains(av.AttributeId) && attributeValueList.Any(value => av.Value.Contains(value)));



            // Return this list
            return contentChannelItemList;

        }

        #endregion

        #region ContentChannelItemsByCampusId
        /// <summary>
        /// Returns a list of content channel items filtered by a campusId
        /// </summary>
        /// <param name="campusId"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/ContentChannelItemsByCampusId")]
        public IQueryable<ContentChannelItem> ContentChannelItemsByAttributeValue(int campusId)
        {
            RockContext rockContext = new RockContext();

            string campusGuid = new CampusService(rockContext).Get(campusId).Guid.ToString();

            // Get the Id of the Rock.Model.ContentChannelItem Entity.
            int contentChannelItemEntityTypeId = EntityTypeCache.Get("Rock.Model.ContentChannelItem").Id;

            // Get the Field Type (Attribute Type) Id of the Data View Field Type.
            int fieldTypeId = FieldTypeCache.Get(Rock.SystemGuid.FieldType.CAMPUSES.AsGuid()).Id;

            // Get the list of attributes that are of the Rock.Model.ContentChannelItem entity type
            // and that are of the Campus field type.
            List<int> campusAttributeIdList = new AttributeService(rockContext)
                .GetByEntityTypeId(contentChannelItemEntityTypeId)
                .Where(item => item.FieldTypeId == fieldTypeId)
                .Select(a => a.Id)
                .ToList();

            List<string> campusAttributeIdKeyList = new AttributeService(rockContext)
                .GetByEntityTypeId(contentChannelItemEntityTypeId)
                .Where(item => item.FieldTypeId == fieldTypeId)
                .Select(a => a.Key)
                .ToList();

            var avsWithCampus = new AttributeValueService(rockContext).Queryable()
                .Where(a => campusAttributeIdList.Contains(a.AttributeId))
                .Where(a => a.Attribute.EntityTypeId == contentChannelItemEntityTypeId)
                .Where(a => a.Value.Contains(campusGuid))
                .Select(a => a.EntityId);



            // I want a list of content channel items whose ids match up to attribute values that represent entity ids
            IQueryable<ContentChannelItem> contentChannelItemList = new ContentChannelItemService(rockContext)
                .Queryable()
                .AsNoTracking()
                .Where(c => avsWithCampus.Contains(c.Id) || c.Attributes.Keys.Contains(campusAttributeIdKeyList.FirstOrDefault()));



            // Return this list
            return contentChannelItemList;

        }

        #endregion

        #region ContentChannelItemsByAttributeValue
        /// <summary>
        /// Returns a list of content channel items filtered by an attribute valu
        /// </summary>
        /// <param name="attributeValues"></param>
        /// <param name="attributeKey"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/ContentChannelItemsByAttributeValue")]
        public IQueryable<ContentChannelItem> ContentChannelItemsByAttributeValue(string attributeValues, string attributeKey)
        {
            RockContext rockContext = new RockContext();

            // Turn the comma separated list of guids into a list of strings.
            List<string> attributeValueList = (attributeValues ?? "").Split(',').ToList();

            // Get the Id of the Rock.Model.ContentChannelItem Entity.
            int contentChannelItemEntityTypeId = EntityTypeCache.Get("Rock.Model.ContentChannelItem").Id;

            // Get the list of attributes that are of the Rock.Model.ContentChannelItem entity type
            // and that are named the the same as the incoming attributeName
            List<int> attributeIdList = new AttributeService(rockContext)
                .GetByEntityTypeId(contentChannelItemEntityTypeId)
                .Where(item => item.Key == attributeKey)
                .Select(a => a.Id)
                .ToList();


            // I want a list of content channel items whose ids match up to attribute values that represent entity ids
            IQueryable<ContentChannelItem> contentChannelItemList = new ContentChannelItemService(rockContext)
                .Queryable()
                .AsNoTracking()
                .WhereAttributeValue(rockContext, av => attributeIdList.Contains(av.AttributeId) && attributeValueList.Any(value => av.Value.Contains(value)));



            // Return this list
            return contentChannelItemList;

        }

        #endregion


        #region ContentChannelItems/GetByCurrentPerson

        /// <summary>
        /// Returns a list of content channel items filtered by the permissions of the current person.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/ContentChannelItems/GetByCurrentPerson")]
        public IQueryable<ContentChannelItem> GetByCurrentPerson( string contentChannelIds )
        {
            // Split the given string of Content Channel Ids into a list and query for 
            // Content Channel Items inside of those Content Channels
            List<int> contentChannelIdList = contentChannelIds.Split(',').Select(int.Parse).ToList();

            // If no Content Channel Ids were given, just return an empty result so make
            // life easier for Rock
            if (contentChannelIdList.Count < 1)
            {
                return Enumerable.Empty<ContentChannelItem>().AsQueryable();
            }

            // Get current person and set up the proxy for querying
            var person = GetPerson();
            RockContext rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;

            // Get all Content Channel Items for the given Content Channels
            IQueryable<ContentChannelItem> contentChannelItemlist = new ContentChannelItemService(rockContext)
                .Queryable()
                .AsNoTracking()
                .Where(item => contentChannelIdList.Contains(item.ContentChannelId));
            
            // Filter results by the current person's permission
            return contentChannelItemlist
                .ToList()
                .Where(item => item.IsAuthorized(Rock.Security.Authorization.VIEW, person) )
                .AsQueryable();
        }

        #endregion

    }
}
