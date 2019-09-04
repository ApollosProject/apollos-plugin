<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ApollosUser.ascx.cs" Inherits="RockWeb.Plugins.apollosproject.ApollosPlugin.ApollosAudit" %>

<ContentTemplate>
    <div class="panel panel-block">
        <div class="panel-heading">
            <h1 class="panel-title"><i class="fa fa-flask"></i>Apollos API User</h1>
        </div>
        <div class="panel-body">
            <Rock:NotificationBox ID="errorMessage" runat="server" NotificationBoxType="Danger" />
           <asp:Panel ID="noUser" runat="server">
                <p>There is curently no Apollos API User/Rest Key. Don't worry! Click the button below to create one.</p>
            <Rock:BootstrapButton
                ID="createApollosApiUser" runat="server" Text="Create Apollos API User" DataLoadingText="Saving..."
                CssClass="btn btn-primary" OnClick="btnCreateApollosApiUserClick" />
            </asp:Panel>
           <asp:Panel ID="userExists" runat="server">
               <p>You will need your API key to connect your Rock instance to your Apollos API. Keep it secret! The API key will grant all access the API User has.</p>
            <Rock:BootstrapButton
                ID="ShowApiKey" runat="server" Text="Show API Key" DataLoadingText="Revealing..."
                CssClass="btn btn-primary" OnClick="btnShowApiKey" />
            <Rock:RockTextBox ID="apiKey" ReadOnly="true" runat="server" Label="Api Key"></Rock:RockTextBox>
            </asp:Panel>
        </div>
    </div>
</ContentTemplate>