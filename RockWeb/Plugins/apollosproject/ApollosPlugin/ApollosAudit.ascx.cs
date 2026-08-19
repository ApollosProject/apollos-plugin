using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace RockWeb.Plugins.apollosproject.ApollosPlugin
{
    [DisplayName( "Apollos Cluster Settings" )]
    [Category( "apollosproject > Admin" )]
    [Description( "Configure Apollos Cluster credentials and options." )]

    // Admins may pick the Defined Type (optional)
    [DefinedTypeField(
        "Config Defined Type",
        Description = "Defined Type that contains the Apollos config Defined Values.",
        IsRequired = false,
        Order = 0,
        Key = "ConfigDefinedType"
    )]

    // If not set above, we fall back to this exact name
    [TextField(
        "Config DefinedType Name",
        Description = "If 'Config Defined Type' is not set, resolve by this Defined Type name.",
        IsRequired = false,
        DefaultValue = "Apollos Plugin",
        Order = 1,
        Key = "ConfigDefinedTypeName"
    )]

    // Default selection by the DefinedValue's *Value* (exact, e.g., 'Production' or 'Development')
    [TextField(
        "Default Config Value Name",
        Description = "Optional: the Defined Value to select by default (e.g., 'Production' or 'Development').",
        IsRequired = false,
        DefaultValue = "Development",
        Order = 2,
        Key = "DefaultConfigValueName"
    )]
    public partial class ApollosAudit : RockBlock
    {
        #region Helpers

        private int? GetDefinedTypeIdByExactName( string typeName )
        {
            if ( string.IsNullOrWhiteSpace( typeName ) )
            {
                return null;
            }

            // Prefer cache
            var dt = DefinedTypeCache.All()
                .FirstOrDefault( d => d.Name == typeName );
            if ( dt != null )
            {
                return dt.Id;
            }

            // DB fallback
            using ( var rc = new RockContext() )
            {
                var svc = new DefinedTypeService( rc );
                return svc.Queryable()
                          .Where( d => d.Name == typeName )
                          .Select( d => ( int? ) d.Id )
                          .FirstOrDefault();
            }
        }

        private int? GetDefinedValueIdByExactValue( int definedTypeId, string value )
        {
            if ( definedTypeId <= 0 || string.IsNullOrWhiteSpace( value ) )
            {
                return null;
            }

            using ( var rc = new RockContext() )
            {
                var dvSvc = new DefinedValueService( rc );

                // Exact match on Value; rely on your data being 'Development' or 'Production'
                return dvSvc.Queryable()
                    .Where( v => v.DefinedTypeId == definedTypeId && v.Value == value )
                    .Select( v => ( int? ) v.Id )
                    .FirstOrDefault();
            }
        }

        #endregion

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // 1) Prefer explicit type from block attribute (GUID)
            var dtGuid = GetAttributeValue( "ConfigDefinedType" ).AsGuidOrNull();
            if ( dtGuid.HasValue )
            {
                var dt = DefinedTypeCache.Get( dtGuid.Value );
                if ( dt != null )
                {
                    dvpConfig.DefinedTypeId = dt.Id;
                    return;
                }
            }

            // 2) Fallback to exact name "Apollos Plugin" (or whatever the attribute says)
            var fallbackTypeName = GetAttributeValue( "ConfigDefinedTypeName" ) ?? "Apollos Plugin";
            var dtId = GetDefinedTypeIdByExactName( fallbackTypeName );
            if ( dtId.HasValue )
            {
                dvpConfig.DefinedTypeId = dtId.Value;
            }
            else
            {
                nb.Visible = true;
                nb.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Danger;
                nb.Text = $"Defined Type '{fallbackTypeName}' was not found.";
            }
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                // Default to "Production" unless admin sets differently
                var defaultValueName = GetAttributeValue( "DefaultConfigValueName" ); // e.g., "Production" or "Development"

                var boundDtId = dvpConfig.DefinedTypeId;
                if ( boundDtId.HasValue && !string.IsNullOrWhiteSpace( defaultValueName ) )
                {
                    var defaultId = GetDefinedValueIdByExactValue( boundDtId.Value, defaultValueName );
                    if ( defaultId.HasValue )
                    {
                        // When bound by DefinedTypeId, SetValue expects the DV Id as a string
                        dvpConfig.SetValue( defaultId.Value.ToString() );
                    }
                }

                LoadForm();
            }
        }

        protected void btnLoad_Click( object sender, EventArgs e ) => LoadForm();

        private void LoadForm()
        {
            nb.Visible = false;

            // Use SelectedValueAsId() to get the selected DefinedValue Id
            int? dvId = dvpConfig.SelectedValueAsId();
            if ( !dvId.HasValue )
            {
                nb.Visible = true;
                nb.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Warning;
                nb.Text = "Select a configuration to load.";
                ClearInputs();
                return;
            }

            var dv = DefinedValueCache.Get( dvId.Value );
            if ( dv == null )
            {
                nb.Visible = true;
                nb.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Danger;
                nb.Text = "Selected configuration not found.";
                ClearInputs();
                return;
            }

            // Read attribute values
            tbBaseUrl.Text = dv.GetAttributeValue( "BaseUrl" ) ?? "https://cluster.apollos.app";
            tbChurchSlug.Text = dv.GetAttributeValue( "ChurchSlug" ) ?? string.Empty;
            tbApiKey.Text = dv.GetAttributeValue( "ApiKey" ); // EncryptedText handled by Rock
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            nb.Visible = false;

            // Use SelectedValueAsId() when reading the selection
            int? dvId = dvpConfig.SelectedValueAsId();
            if ( !dvId.HasValue )
            {
                nb.Visible = true;
                nb.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Warning;
                nb.Text = "Select a configuration to save.";
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var dv = new DefinedValueService( rockContext ).Get( dvId.Value );
                if ( dv == null )
                {
                    nb.Visible = true;
                    nb.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Danger;
                    nb.Text = "Selected configuration not found.";
                    return;
                }

                dv.LoadAttributes( rockContext );

                dv.SetAttributeValue( "BaseUrl", tbBaseUrl.Text.Trim() );
                dv.SetAttributeValue( "ChurchSlug", tbChurchSlug.Text.Trim() );
                dv.SetAttributeValue( "ApiKey", tbApiKey.Text ); // EncryptedText field type

                dv.SaveAttributeValues( rockContext );
            }

            nb.Visible = true;
            nb.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Success;
            nb.Text = "Settings saved.";
        }

        private void ClearInputs()
        {
        }
    }
}
