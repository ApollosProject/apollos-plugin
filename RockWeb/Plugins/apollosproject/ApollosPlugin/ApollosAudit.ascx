<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ApollosAudit.ascx.cs" Inherits="RockWeb.Plugins.apollosproject.ApollosPlugin.ApollosAudit" %>
<Rock:NotificationBox ID="nb" runat="server" Dismissable="true" />

<div class="panel panel-block">
  <div class="panel-heading">
    <h1 class="panel-title"><i class="fa fa-cog"></i> Apollos Cluster Configuration</h1>
  </div>
  <div class="panel-body">
    <div class="row">
      <div class="col-md-12">
        <Rock:DefinedValuePicker ID="dvpConfig" runat="server" Label="Configuration"
          Required="true" Help="Pick which configuration to edit (Defined Value)." />

        <hr />

        <Rock:RockTextBox ID="tbBaseUrl" runat="server" Label="Base URL" Help="e.g., https://cluster.apollos.app" />
        <Rock:RockTextBox ID="tbChurchSlug" runat="server" Label="Church Slug" Help="e.g., your_church_slug" />
        <Rock:RockTextBox ID="tbApiKey" runat="server" Label="API Key" TextMode="Password" />

        <div class="mt-3">
          <Rock:BootstrapButton ID="btnLoad" runat="server" CssClass="btn btn-default" Text="Reload" OnClick="btnLoad_Click" />
          <Rock:BootstrapButton ID="btnSave" runat="server" CssClass="btn btn-primary" Text="Save Settings" OnClick="btnSave_Click" />
        </div>
      </div>
    </div>
  </div>
</div>