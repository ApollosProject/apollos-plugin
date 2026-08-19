using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;
using Rock.Web.Cache;

namespace apollosproject.ApollosPlugin.Jobs
{
[System.ComponentModel.DisplayName( "Apollos Finance Sync" )]
[System.ComponentModel.Description( "Starts an Apollos cluster finance sync and polls until it completes." )]
[TextField(
    "Config DefinedType Name",
    Description = "Name of the Defined Type that contains the Apollos config values (e.g., 'Apollos Plugin').",
    DefaultValue = "Apollos Plugin",
    IsRequired = true,
    Order = 0,
    Key = "ConfigDefinedTypeName"
)]
[TextField(
    "Config Value Name",
    Description = "Exact Defined Value text to use (e.g., 'Production' or 'Development').",
    DefaultValue = "Production",
    IsRequired = true,
    Order = 1,
    Key = "ConfigValueName"
)]
public class StripeSyncJob : RockJob
{
    public override void Execute()
    {
        var typeName = ( GetAttributeValue( "ConfigDefinedTypeName" ) ?? "" ).Trim();
        var valueName = ( GetAttributeValue( "ConfigValueName" ) ?? "" ).Trim();
        string jobId = null;

        if ( string.IsNullOrWhiteSpace( typeName ) )
        {
            UpdateStatusSnapshot(
                status: "failed",
                progress: 0,
                matchErrors: 0,
                syncChargeErrors: 0,
                syncSubscriptionErrors: 0,
                errors: new List<string> { "Config DefinedType Name is required." },
                jobId: jobId
            );
            return;
        }
        if ( string.IsNullOrWhiteSpace( valueName ) )
        {
            UpdateStatusSnapshot(
                status: "failed",
                progress: 0,
                matchErrors: 0,
                syncChargeErrors: 0,
                syncSubscriptionErrors: 0,
                errors: new List<string> { "Config Value Name is required." },
                jobId: jobId
            );
            return;
        }

        // Resolve DefinedType by NAME (cache first)
        var dt = DefinedTypeCache.All().FirstOrDefault( t => t.Name == typeName );
        if ( dt == null )
        {
            UpdateStatusSnapshot(
                status: "failed",
                progress: 0,
                matchErrors: 0,
                syncChargeErrors: 0,
                syncSubscriptionErrors: 0,
                errors: new List<string> { $"Defined Type '{typeName}' not found." },
                jobId: jobId
            );
            return;
        }

        DefinedValue dv;
        using ( var rc = new RockContext() )
        {
            var dvSvc = new DefinedValueService( rc );
            dv = dvSvc.Queryable()
                      .Where( v => v.DefinedTypeId == dt.Id && v.Value == valueName )
                      .FirstOrDefault();

            if ( dv == null )
            {
                UpdateStatusSnapshot(
                    status: "failed",
                    progress: 0,
                    matchErrors: 0,
                    syncChargeErrors: 0,
                    syncSubscriptionErrors: 0,
                    errors: new List<string> { $"Defined Value '{valueName}' not found under '{typeName}'." },
                    jobId: jobId
                );
                return;
            }

            dv.LoadAttributes( rc );
        }

        // Pull config from DV attributes
        var apiKey = dv.GetAttributeValue( "ApiKey" );
        var church = dv.GetAttributeValue( "ChurchSlug" ); // used in x-church header
        var baseUrl = ( dv.GetAttributeValue( "BaseUrl" ) ?? "https://cluster.apollos.app" ).TrimEnd( '/' );

        var timeoutSecs = 60;
        var pollIntervalSeconds = 3;   // How often to poll GET status
        var maxPollAttempts = 200;     // ~10 minutes max (200 * 3s)

        if ( string.IsNullOrWhiteSpace( apiKey ) )
        {
            UpdateStatusSnapshot(
                status: "failed",
                progress: 0,
                matchErrors: 0,
                syncChargeErrors: 0,
                syncSubscriptionErrors: 0,
                errors: new List<string> { "API Key is required in the selected config." },
                jobId: jobId
            );
            return;
        }

        if ( string.IsNullOrWhiteSpace( church ) )
        {
            UpdateStatusSnapshot(
                status: "failed",
                progress: 0,
                matchErrors: 0,
                syncChargeErrors: 0,
                syncSubscriptionErrors: 0,
                errors: new List<string> { "Church Slug is required in the selected config." },
                jobId: jobId
            );
            return;
        }

        var startSyncUrl = $"{baseUrl}/finance/rock/sync";

        try
        {
            using ( var client = new HttpClient { Timeout = TimeSpan.FromSeconds( timeoutSecs ) } )
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
                client.DefaultRequestHeaders.Add( "x-api-key", apiKey );
                client.DefaultRequestHeaders.Add( "x-church", church );

                // 1) Start the sync job (POST /finance/rock/sync)
                using ( var resp = client.PostAsync( startSyncUrl, CreateEmptyJson() ).GetAwaiter().GetResult() )
                {
                    var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    EnsureSuccessOrThrow( resp, body, "Start Finance Sync" );

                    jobId = ExtractJobId( body );
                    if ( string.IsNullOrWhiteSpace( jobId ) )
                    {
                        throw new Exception( "Finance sync start succeeded but no jobId was returned." );
                    }
                }

                // Initial snapshot before we have real data from polling
                UpdateStatusSnapshot(
                    status: "active",
                    progress: 0,
                    matchErrors: 0,
                    syncChargeErrors: 0,
                    syncSubscriptionErrors: 0,
                    errors: new List<string>(),
                    jobId: jobId
                );

                // 2) Poll for job status until it completes / fails (GET /finance/rock/sync/{jobId})
                var statusUrl = $"{baseUrl}/finance/rock/sync/{jobId}";
                int attempts = 0;

                while ( true )
                {
                    attempts++;
                    if ( attempts > maxPollAttempts )
                    {
                        throw new Exception( $"Polling limit reached ({maxPollAttempts} attempts). jobId={jobId}." );
                    }

                    using ( var resp = client.GetAsync( statusUrl ).GetAwaiter().GetResult() )
                    {
                        var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        EnsureSuccessOrThrow( resp, body, "Check Finance Sync Status" );

                        using ( var doc = JsonDocument.Parse( body ) )
                        {
                            var root = doc.RootElement;

                            var status = GetString( root, "status", "unknown" );
                            var progress = GetInt( root, "progress", 0 );

                            var data = root.TryGetProperty( "data", out var dataEl ) && dataEl.ValueKind == JsonValueKind.Object
                                ? dataEl
                                : default( JsonElement );

                            var matchErrors = GetInt( data, "matchErrors", 0 );
                            var syncChargeErrors = GetInt( data, "syncChargeErrors", 0 );
                            var syncSubscriptionErrors = GetInt( data, "syncSubscriptionErrors", 0 );
                            var errorsList = BuildErrorsList( data );

                            // This is the ONLY thing we show in Last Status Message
                            UpdateStatusSnapshot(
                                status: status,
                                progress: progress,
                                matchErrors: matchErrors,
                                syncChargeErrors: syncChargeErrors,
                                syncSubscriptionErrors: syncSubscriptionErrors,
                                errors: errorsList,
                                jobId: jobId
                            );

                            if ( !status.Equals( "active", StringComparison.OrdinalIgnoreCase ) )
                            {
                                // completed OR failed (or any other terminal state) → break
                                break;
                            }
                        }
                    }

                    Thread.Sleep( pollIntervalSeconds * 1000 );
                }
            }
        }
        catch ( Exception ex )
        {
            var msg = ex.Message ?? ex.ToString();
            if ( msg.Length > 300 )
            {
                msg = msg.Substring( 0, 300 ) + "...";
            }

            UpdateStatusSnapshot(
                status: "failed",
                progress: 0,
                matchErrors: 0,
                syncChargeErrors: 0,
                syncSubscriptionErrors: 0,
                errors: new List<string> { msg },
                jobId: jobId
            );
        }
    }

    // --------------------------- helpers -----------------------------------

    private static HttpContent CreateEmptyJson() =>
        new StringContent( "{}", Encoding.UTF8, "application/json" );

    /// <summary>
    /// POST /finance/rock/sync returns the raw job id, e.g. "21".
    /// </summary>
    private static string ExtractJobId( string body )
    {
        if ( string.IsNullOrWhiteSpace( body ) )
        {
            return null;
        }

        var trimmed = body.Trim();
        // If you want to enforce numeric-only jobIds:
        // if ( !long.TryParse( trimmed, out _ ) ) return null;
        return trimmed;
    }

    private static string GetString( JsonElement obj, string propertyName, string defaultValue )
    {
        try
        {
            if ( obj.ValueKind == JsonValueKind.Object &&
                 obj.TryGetProperty( propertyName, out var prop ) &&
                 prop.ValueKind == JsonValueKind.String )
            {
                return prop.GetString();
            }
        }
        catch
        {
        }
        return defaultValue;
    }

    private static int GetInt( JsonElement obj, string propertyName, int defaultValue )
    {
        try
        {
            if ( obj.ValueKind == JsonValueKind.Object &&
                 obj.TryGetProperty( propertyName, out var prop ) &&
                 prop.ValueKind == JsonValueKind.Number &&
                 prop.TryGetInt32( out var value ) )
            {
                return value;
            }
        }
        catch
        {
        }
        return defaultValue;
    }

    /// <summary>
    /// Build a compact errors list from data.errors[].
    /// Dedupe and count same messages, e.g. "msg (x23)".
    /// Only adds "+N more" when there are additional distinct messages
    /// beyond the ones displayed.
    /// </summary>
    private static List<string> BuildErrorsList( JsonElement data )
    {
        var result = new List<string>();

        if ( data.ValueKind != JsonValueKind.Object ||
             !data.TryGetProperty( "errors", out var errorsEl ) ||
             errorsEl.ValueKind != JsonValueKind.Array )
        {
            return result;
        }

        var messages = errorsEl.EnumerateArray()
            .Where( e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty( "message", out _ ) )
            .Select( e =>
            {
                try
                {
                    return e.GetProperty( "message" ).GetString() ?? "";
                }
                catch
                {
                    return "";
                }
            } )
            .Where( m => !string.IsNullOrWhiteSpace( m ) )
            .ToList();

        if ( messages.Count == 0 )
        {
            return result;
        }

        // Count occurrences per distinct message
        var countsByMessage = messages
            .GroupBy( m => m )
            .ToDictionary( g => g.Key, g => g.Count() );

        const int maxDistinctToShow = 10;

        var orderedDistinct = countsByMessage
            .OrderByDescending( x => x.Value )
            .ThenBy( x => x.Key )
            .ToList();

        var shownDistinct = orderedDistinct.Take( maxDistinctToShow ).ToList();

        foreach ( var kvp in shownDistinct )
        {
            var msg = kvp.Key;
            var count = kvp.Value;
            var text = count > 1 ? $"{msg} (x{count})" : msg;
            result.Add( text );
        }

        var totalDistinct = orderedDistinct.Count;
        var shownDistinctCount = shownDistinct.Count;

        // Only add "+N more" if there are more distinct messages than we showed
        if ( totalDistinct > shownDistinctCount )
        {
            var hiddenDistinctCount = totalDistinct - shownDistinctCount;
            result.Add( $"+{hiddenDistinctCount} more" );
        }

        return result;
    }

    /// <summary>
    /// Single, consistent HTML snapshot for Last Status Message.
    /// </summary>
    private void UpdateStatusSnapshot(
        string status,
        int progress,
        int matchErrors,
        int syncChargeErrors,
        int syncSubscriptionErrors,
        List<string> errors,
        string jobId )
    {
        errors = errors ?? new List<string>();

        // HTML encode dynamic bits so weird characters don't break markup
        string E( string s ) => WebUtility.HtmlEncode( s ?? string.Empty );

        var sb = new StringBuilder();

        sb.AppendLine( "<ul>" );
        sb.AppendLine( $"  <li><strong>Status:</strong> {E( status )}</li>" );
        sb.AppendLine( $"  <li><strong>Progress:</strong> {progress}%</li>" );
        sb.AppendLine( $"  <li><strong>Match Errors:</strong> {matchErrors}</li>" );
        sb.AppendLine( $"  <li><strong>Sync Charge Errors:</strong> {syncChargeErrors}</li>" );
        sb.AppendLine( $"  <li><strong>Sync Subscription Errors:</strong> {syncSubscriptionErrors}</li>" );
        sb.AppendLine( $"  <li><strong>Reference Number:</strong> {E( string.IsNullOrWhiteSpace( jobId ) ? "N/A" : jobId )}</li>" );

        if ( errors.Count == 0 )
        {
            sb.AppendLine( "  <li><strong>Errors:</strong> None</li>" );
        }
        else
        {
            sb.AppendLine( "  <li><strong>Errors:</strong>" );
            sb.AppendLine( "    <ul>" );
            foreach ( var err in errors )
            {
                sb.AppendLine( $"      <li>{E( err )}</li>" );
            }
            sb.AppendLine( "    </ul>" );
            sb.AppendLine( "  </li>" );
        }

        sb.AppendLine( "</ul>" );

        UpdateLastStatusMessage( sb.ToString() );
    }

    private static void EnsureSuccessOrThrow( HttpResponseMessage response, string body, string stepName )
    {
        if ( response.IsSuccessStatusCode )
        {
            return;
        }

        var safeBody = body ?? string.Empty;
        if ( safeBody.Length > 500 )
        {
            safeBody = safeBody.Substring( 0, 500 ) + "...";
        }

        var msg = $"{stepName} failed: {( int ) response.StatusCode} {response.ReasonPhrase}. Body: {safeBody}";
        throw new Exception( msg );
    }
}
}
