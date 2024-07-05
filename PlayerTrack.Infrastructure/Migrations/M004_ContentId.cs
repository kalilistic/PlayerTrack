using Dalamud.DrunkenToad.Core;
using FluentDapperLite.Extension;
using FluentMigrator;

namespace PlayerTrack.Repositories.Migrations;

[Migration(20240705200000)]
public class M004_ContentId: Migration
{
    public override void Up()
    {
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