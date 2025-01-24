using FluentDapperLite.Extension;
using FluentMigrator;

namespace PlayerTrack.Repositories.Migrations;

[Migration(20240604015600)]
public class M003_LodestoneAPI : FluentMigrator.Migration
{
    public override void Up()
    {
        Alter.Table("lodestone_lookups")
            .AddColumn("world_id").AsUInt32("world_id").NotNullable().WithDefaultValue(0)
            .AddColumn("updated_player_name").AsString().NotNullable().WithDefaultValue(string.Empty)
            .AddColumn("updated_world_id").AsUInt32("updated_world_id").NotNullable().WithDefaultValue(0)
            .AddColumn("lookup_type").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("is_done").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("prerequisite_lookup_id").AsInt32().Nullable();
        Create.Index("idx_lodestone_lookups_lookup_type").OnTable("lodestone_lookups").OnColumn("lookup_type").Ascending();
        Create.ForeignKey("fk_lodestone_lookups_prerequisite_lookup_id")
            .FromTable("lodestone_lookups").ForeignColumn("prerequisite_lookup_id")
            .ToTable("lodestone_lookups").PrimaryColumn("id");
        Execute.Sql(@"
            UPDATE lodestone_lookups
            SET is_done = 'true'
            WHERE lookup_status IN (1, 3)
        ");
        foreach (var world in Sheets.Worlds.Values)
        {
            Execute.Sql($@"
                UPDATE lodestone_lookups
                SET world_id = {world.Id}
                WHERE world_name = '{world.Name.Replace("'", "''")}'
            ");
        }
        Execute.Sql(@"
            UPDATE lodestone_lookups
            SET lookup_status = 6, is_done = 'true'
            WHERE world_id = 0
        ");
    }

    public override void Down()
    {
        Delete.ForeignKey("fk_lodestone_lookups_prerequisite_lookup_id").OnTable("lodestone_lookups");
        Delete.Index("idx_lodestone_lookups_lookup_type").OnTable("lodestone_lookups");
        Delete.Column("prerequisite_lookup_id").FromTable("lodestone_lookups");
        Delete.Column("is_done").FromTable("lodestone_lookups");
        Delete.Column("world_id").FromTable("lodestone_lookups");
        Delete.Column("lookup_type").FromTable("lodestone_lookups");
    }
}
