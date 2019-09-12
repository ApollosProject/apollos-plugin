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
            List<int> idList = ids.Split(',').Select(int.Parse).ToList();
            IQueryable<ContentChannelItem> contentChannelItemList = new ContentChannelItemService(rockContext).Queryable().Where(item => idList.Contains(item.Id)) ;

            return contentChannelItemList;
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
            RockContext rockContext = new RockContext();

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

            // Get the data view guids from the DataViewPersistedValues table that the Person Id is a part of
            var persistedValuesQuery = rockContext.DataViewPersistedValues.Where(a => a.EntityId == entityId && a.DataView.EntityTypeId == entityTypeId);
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
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery]
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Apollos/GetEventItemOccurencesByCalendarId")]
        public IQueryable<EventItemOccurrence> GetEventItemOccurencesByCalendarId(int id)
        {
            RockContext rockContext = new RockContext();
            List<int> itemOccurenceIds = new EventCalendarItemService(rockContext).Queryable().Where(item => id == item.EventCalendarId).Select(item => item.EventItemId).ToList();
            IQueryable<EventItemOccurrence> itemOccurences = new EventItemOccurrenceService(rockContext).Queryable().Where(item => itemOccurenceIds.Contains(item.Id));
            return itemOccurences;
        }
        #endregion

    }
}
