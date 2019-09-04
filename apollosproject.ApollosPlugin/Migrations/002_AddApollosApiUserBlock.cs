using Rock;
using Rock.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apollosproject.ApollosPlugin.Migrations
{
    [MigrationNumber(2, "1.0.13")]
    class AddApollosApiUserBlock : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.UpdateBlockType("Apollos App User Configuration", "A block to help setup the Apollos API user", "~/Plugins/apollosproject/ApollosPlugin/ApollosUser.ascx", "Apollos App", "9B433484-D4A0-4DDD-9B91-3BD229F38A74");
            RockMigrationHelper.AddBlock(true, "1E0A8C5E-5592-44DC-8B11-8940875C7FCC".AsGuid(), null, "C2D29296-6A87-47A9-A753-EE4E9159C4C4".AsGuid(), "9B433484-D4A0-4DDD-9B91-3BD229F38A74".AsGuid(), "Apollos", "SectionC", @"", @"", 0, "C281CE8A-CCC9-41DF-AC70-71D1F95EF2E5");
        }
        public override void Down()
        {
            // Remove Block: Apollos App Setup Info, from Page: Apollos Dashboard, Site: Rock RMS
            RockMigrationHelper.DeleteBlock("CE7D68A1-E69D-4A6D-9897-A702B0B019D2");
            // Apollos App User Configuration
            RockMigrationHelper.DeleteBlockType("9B433484-D4A0-4DDD-9B91-3BD229F38A74"); 
        }
    }
}
