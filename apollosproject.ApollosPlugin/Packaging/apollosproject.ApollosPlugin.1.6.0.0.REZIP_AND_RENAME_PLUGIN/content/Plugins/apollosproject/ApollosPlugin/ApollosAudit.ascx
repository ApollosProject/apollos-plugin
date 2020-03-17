<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ApollosAudit.ascx.cs" Inherits="RockWeb.Plugins.apollosproject.ApollosPlugin.ApollosAudit" %>

<ContentTemplate>
    <div class="panel panel-block">
        <div class="panel-heading">
            <h1 class="panel-title"><i class="fa fa-flask"></i> REST Controller Settings</h1>
        </div>
        <div class="panel-body">
            <Rock:NotificationBox ID="GetCurrentPersonEnabled" runat="server" NotificationBoxType="Default" />
            <p>The Apollos Project uses the</p><pre> api/People/GetCurrentPerson </pre><p>RestAction in order to fetch the current signed in user's profile. By default, this REST endpoint is not accessible to anyone but Admins. We need to unblock this endpoint so that all users are able to fetch their own profiles.</p>              
            <Rock:BootstrapButton
                ID="enableGetCurrentPerson" runat="server" Text="Unblock Get Current Person" DataLoadingText="Saving..."
                CssClass="btn btn-primary" OnClick="btnEnableGetCurrentPerson_Click" />
        </div>
    </div>
</ContentTemplate>