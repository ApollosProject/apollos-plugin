using Rock;
using Rock.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apollosproject.ApollosPlugin.Migrations
{
    class AddPage : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddPage(true, "550A898C-EDEA-48B5-9C58-B20EC13AF13B", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Apollos Dashboard", "", "1E0A8C5E-5592-44DC-8B11-8940875C7FCC", ""); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType("Apollos App Setup Info", "A block to help debug common issues with Rock/Apollos App Set", "~/Plugins/apollosproject/ApollosPlugin/ApollosAudit.ascx", "Apollos App", "9414B495-AF47-4447-B24C-CE884D1E7409");
            RockMigrationHelper.AddBlock( true, "1E0A8C5E-5592-44DC-8B11-8940875C7FCC".AsGuid(),null,"C2D29296-6A87-47A9-A753-EE4E9159C4C4".AsGuid(),"9414B495-AF47-4447-B24C-CE884D1E7409".AsGuid(), "Apollos App Setup Info","SectionB",@"",@"",0,"CE7D68A1-E69D-4A6D-9897-A702B0B019D2");
        }
        public override void Down()
        {
            // Remove Block: Apollos App Setup Info, from Page: Apollos Dashboard, Site: Rock RMS
            RockMigrationHelper.DeleteBlock("CE7D68A1-E69D-4A6D-9897-A702B0B019D2");
            RockMigrationHelper.DeleteBlockType("9414B495-AF47-4447-B24C-CE884D1E7409"); // Apollos App Setup Info
            RockMigrationHelper.DeletePage("1E0A8C5E-5592-44DC-8B11-8940875C7FCC"); //  Page: Apollos Dashboard, Layout: Full Width, Site: Rock RMS
        }
    }
}
