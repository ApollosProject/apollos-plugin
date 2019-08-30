﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ApollosUser.ascx.cs" Inherits="RockWeb.Plugins.apollosproject.ApollosPlugin.ApollosAudit" %>

<ContentTemplate>
    <div class="panel panel-block">
        <div class="panel-heading">
            <h1 class="panel-title"><i class="fa fa-flask"></i>Apollos API User</h1>
        </div>
        <div class="panel-body">       
           <asp:Panel ID="noUser" runat="server">
                <p>There is curently no Apollos API User/Rest Key. Don't worry! Click the button below to create one.</p>
            <Rock:BootstrapButton
                ID="createApollosApiUser" runat="server" Text="Create Apollos API User" DataLoadingText="Saving..."
                CssClass="btn btn-primary" OnClick="btnCreateApollosApiUserClick" />
            </asp:Panel>
           <asp:Panel ID="userExists" runat="server">
            <Rock:BootstrapButton
                ID="ShowApiKey" runat="server" Text="Show API Key" DataLoadingText="Revealing..."
                CssClass="btn btn-primary" OnClick="btnShowApiKey" />
            <Rock:RockTextBox ID="apiKey" ReadOnly="true" runat="server" Label="Api Key"></Rock:RockTextBox>
            </asp:Panel>
        </div>
    </div>
</ContentTemplate>