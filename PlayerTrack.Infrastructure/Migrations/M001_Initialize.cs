using FluentDapperLite.Extension;
using FluentMigrator;
using FluentMigrator.Builders.Execute;

namespace PlayerTrack.Repositories.Migrations;

[Migration(20230915162000)]
public class M001_Initialize : Migration
{
    public override void Up()
    {
        this.CreatePlayersTable();
        this.CreateBackupsTable();
        this.CreateCategoriesTable();
        this.CreateEncountersTable();
        this.CreatePlayerEncountersTable();
        this.CreateLodestoneLookupsTable();
        this.CreateTagsTable();
        this.CreatePlayerTagsTable();
        this.CreatePlayerCategoriesTable();
        this.CreatePlayerNameWorldHistoriesTable();
        this.CreatePlayerCustomizeHistoriesTable();
        this.CreateConfigTable();
        this.CreatePlayerConfigTable();
        this.CreateArchiveTable();
    }

    public override void Down()
    {
        this.Delete.Table("players");
        this.Delete.Table("backups");
        this.Delete.Table("categories");
        this.Delete.Table("encounters");
        this.Delete.Table("player_encounters");
        this.Delete.Table("lodestone_lookups");
        this.Delete.Table("player_name_world_histories");
        this.Delete.Table("player_customize_histories");
        this.Delete.Table("tags");
        this.Delete.Table("player_tags");
        this.Delete.Table("player_categories");
        this.Delete.Table("configs");
        this.Delete.Table("player_config");
        this.Delete.Table("archive_records");
    }

    private void CreatePlayersTable()
    {
        this.Create.Table("players")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("last_alert_sent").AsInt64().NotNullable()
            .WithColumn("last_seen").AsInt64().NotNullable()
            .WithColumn("customize").AsBinary().Nullable()
            .WithColumn("seen_count").AsInt32().NotNullable()
            .WithColumn("lodestone_status").AsInt32().NotNullable()
            .WithColumn("lodestone_verified_on").AsInt64().NotNullable()
            .WithColumn("free_company_state").AsString().NotNullable()
            .WithColumn("free_company_tag").AsString().NotNullable()
            .WithColumn("key").AsString().NotNullable()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("notes").AsString().NotNullable()
            .WithColumn("lodestone_id").AsUInt32("lodestone_id").NotNullable()
            .WithColumn("object_id").AsUInt32("object_id").NotNullable()
            .WithColumn("world_id").AsUInt32("world_id").NotNullable()
            .WithColumn("last_territory_type").AsUInt16("last_territory_type").NotNullable();

        this.Create.Index("idx_players_key").OnTable("players").OnColumn("key").Ascending().WithOptions().Unique();
    }

    private void CreateBackupsTable()
    {
        this.Create.Table("backups")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("backup_type").AsInt32().NotNullable()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("size").AsInt64().NotNullable()
            .WithColumn("is_restorable").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("is_protected").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("notes").AsString().Nullable();

        this.Create.Index("idx_backups_name").OnTable("backups").OnColumn("name").Ascending();
    }

    private void CreateCategoriesTable()
    {
        this.Create.Table("categories")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("rank").AsInt32().NotNullable();

        this.Create.Index("idx_categories_name").OnTable("categories").OnColumn("name").Ascending();
        this.Create.Index("idx_categories_rank").OnTable("categories").OnColumn("rank").Ascending();
    }

    private void CreateEncountersTable()
    {
        this.Create.Table("encounters")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("territory_type_id").AsUInt16("territory_type_id").NotNullable()
            .WithColumn("ended").AsInt64().NotNullable();

        this.Create.Index("idx_encounters_territory_type_id").OnTable("encounters").OnColumn("territory_type_id").Ascending();
    }

    private void CreatePlayerEncountersTable()
    {
        this.Create.Table("player_encounters")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("job_id").AsUInt32("job_id").NotNullable()
            .WithColumn("ended").AsInt64().NotNullable()
            .WithColumn("job_lvl").AsByte().NotNullable()
            .WithColumn("player_id").AsInt32().NotNullable().ForeignKey("fk_player_encounters_players", "players", "id")
            .WithColumn("encounter_id").AsInt32().NotNullable().ForeignKey("fk_player_encounters_encounters", "encounters", "id");

        this.Create.Index("idx_player_encounters_encounter_id").OnTable("player_encounters").OnColumn("encounter_id").Ascending();
        this.Create.Index("idx_player_encounters_player_id").OnTable("player_encounters").OnColumn("player_id").Ascending();
    }

    private void CreateLodestoneLookupsTable()
    {
        this.Create.Table("lodestone_lookups")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("player_name").AsString().NotNullable()
            .WithColumn("world_name").AsString().NotNullable()
            .WithColumn("lodestone_id").AsUInt32("lodestone_id").NotNullable()
            .WithColumn("failure_count").AsInt32().NotNullable()
            .WithColumn("lookup_status").AsInt32().NotNullable()
            .WithColumn("player_id").AsInt32().NotNullable().ForeignKey("fk_lodestone_lookups_players", "players", "id");

        this.Create.Index("idx_lodestone_lookups_player_id").OnTable("lodestone_lookups").OnColumn("player_id").Ascending();
        this.Create.Index("idx_lodestone_lookups_lookup_status").OnTable("lodestone_lookups").OnColumn("lookup_status").Ascending();
    }

    private void CreatePlayerNameWorldHistoriesTable()
    {
        this.Create.Table("player_name_world_histories")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("is_migrated").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("player_name").AsString().Nullable()
            .WithColumn("world_id").AsUInt32("world_id").Nullable()
            .WithColumn("player_id").AsInt32().NotNullable().ForeignKey("fk_player_name_world_histories_players", "players", "id");

        this.Create.Index("idx_player_name_world_histories_player_id").OnTable("player_name_world_histories").OnColumn("player_id").Descending();
        this.Create.Index("idx_player_name_world_histories_created").OnTable("player_name_world_histories").OnColumn("created").Descending();
    }

    private void CreatePlayerCustomizeHistoriesTable()
    {
        this.Create.Table("player_customize_histories")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("is_migrated").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("customize").AsBinary().NotNullable()
            .WithColumn("player_id").AsInt32().NotNullable().ForeignKey("fk_player_customize_histories_players", "players", "id");

        this.Create.Index("idx_player_customize_histories_player_id").OnTable("player_customize_histories").OnColumn("player_id").Descending();
        this.Create.Index("idx_player_customize_histories_created").OnTable("player_customize_histories").OnColumn("created").Descending();
    }

    private void CreateTagsTable()
    {
        this.Create.Table("tags")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("color").AsUInt32("color").WithDefaultValue(5);

        this.Create.Index("idx_tags_name").OnTable("tags").OnColumn("name").Ascending();
    }

    private void CreatePlayerTagsTable()
    {
        this.Create.Table("player_tags")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("player_id").AsInt32().ForeignKey("fk_player_tags_players", "players", "id")
            .WithColumn("tag_id").AsInt32().ForeignKey("fk_player_tags_tags", "tags", "id");

        this.Create.Index("idx_player_tags_player_id").OnTable("player_tags").OnColumn("player_id").Ascending();
        this.Create.Index("idx_player_tags_tag_id").OnTable("player_tags").OnColumn("tag_id").Ascending();
    }

    private void CreatePlayerCategoriesTable()
    {
        this.Create.Table("player_categories")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("player_id").AsInt32().ForeignKey("fk_player_categories_players", "players", "id")
            .WithColumn("category_id").AsInt32().ForeignKey("fk_player_categories_categories", "categories", "id");

        this.Create.Index("idx_player_categories_player_id").OnTable("player_categories").OnColumn("player_id").Ascending();
        this.Create.Index("idx_player_categories_category_id").OnTable("player_categories").OnColumn("category_id").Ascending();
    }

    private void CreateConfigTable()
    {
        this.Create.Table("configs")
              .WithColumn("key").AsString().NotNullable().PrimaryKey()
              .WithColumn("value").AsString().NotNullable();
    }

    private void CreatePlayerConfigTable()
    {
        this.Create.Table("player_config")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("player_config_type").AsInt32()
            .WithColumn("player_list_name_color").AsString()
            .WithColumn("player_list_icon").AsString()
            .WithColumn("nameplate_custom_title").AsString()
            .WithColumn("nameplate_show_in_overworld").AsString()
            .WithColumn("nameplate_show_in_content").AsString()
            .WithColumn("nameplate_show_in_high_end_content").AsString()
            .WithColumn("nameplate_color").AsString()
            .WithColumn("nameplate_use_color").AsString()
            .WithColumn("nameplate_use_color_if_dead").AsString()
            .WithColumn("nameplate_title_type").AsString()
            .WithColumn("alert_name_change").AsString()
            .WithColumn("alert_world_transfer").AsString()
            .WithColumn("alert_proximity").AsString()
            .WithColumn("alert_format_include_category").AsString()
            .WithColumn("alert_format_include_custom_title").AsString()
            .WithColumn("visibility_type").AsString()
            .WithColumn("player_id").AsInt32().Nullable().ForeignKey("fk_player_config_players", "players", "id")
            .WithColumn("category_id").AsInt32().Nullable().ForeignKey("fk_player_config_categories", "categories", "id");

        this.Create.Index("idx_player_config_player_id").OnTable("player_config").OnColumn("player_id").Ascending().WithOptions().Unique();
        this.Create.Index("idx_player_config_category_id").OnTable("player_config").OnColumn("category_id").Ascending().WithOptions().Unique();
    }

    private void CreateArchiveTable()
    {
        this.Create.Table("archive_records")
            .WithIdColumn()
            .WithTimeStampColumns()
            .WithColumn("archive_type").AsInt32()
            .WithColumn("data").AsString();
    }
}
