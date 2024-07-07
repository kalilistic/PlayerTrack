using Dalamud.DrunkenToad.Core;
using FluentDapperLite.Extension;
using FluentMigrator;

namespace PlayerTrack.Repositories.Migrations;

[Migration(20240705200000)]
public class M004_ContentId: Migration
{
    public override void Up()
    {
        this.Alter.Table("players")
            .AddColumn("entity_id").AsUInt32("entity_id").NotNullable().WithDefaultValue(0);
        
        // copy old data from object_id to new entity_id
        this.Execute.Sql(@"
            UPDATE players
            SET entity_id = object_id
            WHERE object_id != 0");
        
        // delete old object_id column
        this.Execute.Sql("ALTER TABLE players DROP COLUMN object_id");
        
        // clear bad content ids from bugs
        this.Execute.Sql(@"
            UPDATE players
            SET content_id = 0
            WHERE content_id != 0
        ");
    }
    
    public override void Down()
    {
    }
}