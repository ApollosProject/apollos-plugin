# Apollos Plugin
An API plugin for Rock, the Apollos plugin adds a number of helpful API endpoints for use in development of your Apollos mobile app. 

## Tech Specs
The Apollos plugin is written in C#, currently using version `4.7.2` of the .NET Framework. It is written specifically to work with Rock RMS. You can find out more about Rock at https://www.rockrms.com.

## Setup your development environment
If you’re interested in contributing to this plugin, the first step is to get your development environment up and running. 

**Note**: For the purposes of this documentation, we are going to assume that you have a development environment in which you currently run Rock. 

First things first, clone down this repo. It can technically live anywhere, but we’ve found it easiest to just put it in the same parent folder as your Rock code. This makes it easier to find and reference later.

### In Visual Studio:
* Open up the Apollos plugin solution in Visual Studio.
**Note**:The first time you open the Apollos plugin solution, you’ll want to make sure that your references to anything Rock related are correct and pointing to the right DLL in the `RockWeb\bin` folder. 
* Make sure that you’re building in `Debug` mode and then clean and rebuild the Apollos plugin solution.
* Once that builds, you’ll need to do some copying & pasting of files into your Rock folders.
	* Copy the `apollosproject.ApollosPlugin.dll` from the `apollos-plugin\apollosproject.ApollosPlugin\bin\Debug` folder of the Apollos plugin and paste it into the `RockWeb\bin` folder of your Rock code.
	* Copy the `apollosproject` folder from  `apollos-plugin\RockWeb\Plugins` and paste it into the `RockWeb\Plugins` folder. 
* Then open your Rock solution, clean and build. Once this finishes building go ahead and startup Rock.

**Note**: We should mention here that the Apollos plugin project does not automatically copy down the compiled DLL to your Rock `RockWeb\bin` folder. So every time you make a change to the plugin code and compile, you will need to copy the newly generated DLL to the `RockWeb\bin` folder and probably recompile Rock. This just guarantees that you’re using the latest and greatest version of your plugin code when you run Rock.

* At this point, you should be able to get to the Rock API Docs by going to `Admin Tools > Power Tools > API Docs`. Look for `Apollos` in the sidebar. 

## What am I looking at?
So you’ve got the Apollos plugin open in Visual Studio, but what next?

All of the API endpoints we’ve created so far live in the `ApollosController.cs` file (which can be found in the `Rest` folder. 

Let’s talk a little bit about what a Rock API endpoint looks like.

### Anatomy of an API endpoint
```
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
```

All the endpoints you create should follow this pattern. So what are we looking at here?
* Every endpoint should live in its own region. This keeps everything nicely separated and easily collapsible. 
* It should _at a minimum_ have a `<summary>`. Answer the question “What does this endpoint do?” It can also include callouts for passed in parameters and what the function returns.
* The section of things in brackets are attributes. In this case, we are calling out that this endpoint:
	*  is doing a `GET`
	* it has `OData` querying enabled
	* the user is authenticated and has the right security permissions to use this endpoint
	* and that its route is defined.
* The rest is the function that gets the content channel items and returns them in a list.

## Create a release
Okay, so you’ve gotten the project up and running and you’ve written an endpoint. Now how do we get that endpoint into the Rock Shop and the hands of the folks who need to use it?

Great question. There are a few things that need to happen in order to make that dream a reality.

### Create a release and PR.
Once you’ve verified your endpoint is working, the next step is to create a release build.

* Open up `AssemblyInfo.cs` (it lives under `Properties`) and change the version number at the bottom of the file to whatever the new version should be.
* Change the solution configuration dropdown to be `Release` .
* Clean and rebuild the solution.

When you do this, it will create a zip file with all the necessary things you need to create a new build for the Rock Shop. 

Create a PR and get that bad boy sent up to the `apollos-plugin` repo for review!

### Release to the Rock Shop
Your PR has been approved and merged. Congrats! There’s still one more step to get this into the hands of the people. You have to add a new release to the Rock Shop!

* Sign in to the Rock community website (https://community.rockrms.com)
* From your avatar in the upper right hand corner, choose “Your Organizations”.
* Choose “Differential”
* Go to “Rock Shop Packages” in the left hand sidebar and click on “Apollos Project”.
* At the bottom of the page, click the plus button (+) in the grid.
* Change the extension on the zip file that was created when you built the project in release mode from `.zip` to `.plugin`.
* Fill out the form, making sure to upload the `.plugin` file and adding at least one screenshot. Click `Save`.
* Once you’re ready to go, click `Submit for Review`.

Reading and following the instructions in the _Packaging Plugins and Themes_ documentation (found https://rockrms.blob.core.windows.net/documentation/PDFs/2a8a41e238b94f6587ba2024c8666f00_PackagingPluginsandThemes.pdf) is also a fantastic way to go. 

## API Endpoints
What endpoints are available and what do they do?

### `GetContentChannelItemsByIds`
* **Type**: GET
* **Parameters**: `string ids`
* **Route**: `api/Apollos/GetContentChannelItemsByIds`
* **Description**: Returns a list of content channel items based on a list of content channel item ids.

### `GetInteractionsByForeignKeys`
* **Type**: GET
* **Parameters**: `string keys`
* **Route**: `api/Apollos/GetInteractionsByForeignKeys`
* **Description**: Returns a list of interactions based on a list of foreign keys.

### `ContentChannelItemsByDataViewGuids`
* **Type**: GET
* **Parameters**: `string guids`
* **Route**: `api/Apollos/ContentChannelItemsByDataViewGuids`
* **Description**: Returns a list of content channel items based on a list of dataview guids.

### `GetPersistedDataViewsForEntity`
* **Type**: GET
* **Parameters**: `int entityTypeId, int entityId, System.Guid? categoryGuid, int categoryId`
* **Route**: `api/Apollos/GetPersistedDataViewsForEntity/{entityTypeId}/{entityId}`
* **Description**: Returns a list of dataviews that a person is a part of.

### `GetEventItemOccurencesByCalendarId`
* **Type**: GET
* **Parameters**: `int id`
* **Route**: `api/Apollos/GetEventItemOccurencesByCalendarId`
* **Description**: Returns a list of event item occurrences, filtered by a specific CalendarId

### `ContentChannelItemsByCampusIdAndAttributeValue`
* **Type**: GET
* **Parameters**: `string attributeValues, string attributeKey, int campusId`
* **Route**: `api/Apollos/ContentChannelItemsByCampusIdAndAttributeValue`
* **Description**: Returns a list of content channel items filtered by an attribute value and a campus id

### `ContentChannelItemsByCampusId`
* **Type**: GET
* **Parameters**: `int campusId`
* **Route**: `api/Apollos/ContentChannelItemsByCampusId`
* **Description**: Returns a list of content channel items filtered by a campusId

### `ContentChannelItemsByAttributeValue`
* **Type**: GET
* **Parameters**: `string attributeValues, string attributeKey`
* **Route**: `api/Apollos/ContentChannelItemsByAttributeValue`
* **Description**: Returns a list of content channel items filtered by an attribute value.

### `ContentChannelItems/GetByCurrentPerson`
* **Type**: GET
* **Parameters**: `string contentChannelIds`
* **Route**: `api/Apollos/ContentChannelItems/GetByCurrentPerson`
* **Description**: Returns a list of content channel items filtered by the permissions of the current person.

### `ChangedContentChannelItemsByDate`
* **Type**: GET
* **Parameters**: `DateTime changedSinceDate`
* **Route**: `api/Apollos/ChangedContentChannelItemsByDate`
* **Description**: Returns a list of content channel items that: (a) were created or changed since the passed in date and (b) had attributes that were created or changed since the passed in date.

## Updating via the Rock Shop
If you are updating the plugin from one version to another via the Rock Shop, you should know that as of this writing, downloading the latest version of the plugin does NOT automatically overwrite
the plugin files. In order to update to the latest version properly, you will have to (at a minimum) go into your `Rockweb/bin` folder and delete the `apollosProject.ApollosPlugin.dll` and
`apollosProject.ApollosPlugin.xml` files before downloading the latest version. If the latest version includes an update to the Apollos Dashboard pages in Rock then you should also go into your `Plugins`
folder and delete the `apollosProject` folder. This folder should include the source files for the `ApollosAudit` and `ApollosUser` pages. Don't worry, we'll call this out in the update notes should that happen.

---

## About the Apollos Project
Join a community of churches working together to bring people closer to God and advance the Kingdom through technology.

### Built by the Church, for the Church
The Apollos Project is an open-source initiative that empowers your church to **launch your own digital products** and **bring your congregation closer to Jesus.**

We’re currently focused on building a mobile application platform with a graph-based data layer that **integrates directly with your ChMS** and other services you already use.

> I planted, Apollos watered, but God gave the growth.
> - 1 Corinthians 3:6
