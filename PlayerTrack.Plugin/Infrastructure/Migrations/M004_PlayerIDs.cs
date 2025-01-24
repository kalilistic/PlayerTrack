using FluentDapperLite.Extension;
using FluentMigrator;

namespace PlayerTrack.Repositories.Migrations;

[Migration(20240709120000)]
public class M004_PlayerIDs: FluentMigrator.Migration
{

    public override void Up()
    {
        AddNewFields();
        DeleteObjectId();
        FixContentId();
        FixPlayerEncounterEnded();
        SetNameHistorySource();
        RemovePlayerKeyUniqueConstraint();
    }

    private void AddNewFields()
    {
        Alter.Table("players")
            .AddColumn("entity_id").AsUInt32("entity_id").NotNullable().WithDefaultValue(0);
        Alter.Table("player_name_world_histories")
            .AddColumn("source").AsInt32().NotNullable().WithDefaultValue(0);
    }

    private void DeleteObjectId()
    {
        Execute.Sql(@"
            UPDATE players
            SET entity_id = object_id
            WHERE object_id != 0");
        Execute.Sql("ALTER TABLE players DROP COLUMN object_id");
    }

    private void FixContentId()
    {
        Execute.Sql("DROP INDEX IF EXISTS idx_players_content_id");
        Execute.Sql(@"
            UPDATE players
            SET content_id = 0
            WHERE content_id IS NULL OR content_id != 0
        ");
        Execute.Sql("CREATE INDEX idx_players_content_id ON players (content_id ASC)");
    }

    private void FixPlayerEncounterEnded()
    {
        Execute.Sql(@"
            UPDATE player_encounters
            SET ended = (
                SELECT encounters.ended
                FROM encounters
                WHERE player_encounters.encounter_id = encounters.id
            )
            WHERE player_encounters.ended = 0;
        ");
    }

    private void SetNameHistorySource()
    {
        Execute.Sql(@"
            UPDATE player_name_world_histories
            SET source = 1
            WHERE source = 0
        ");
    }

    private void RemovePlayerKeyUniqueConstraint()
    {
        Execute.Sql("DROP INDEX IF EXISTS idx_players_key");
        Execute.Sql("CREATE INDEX idx_players_key ON players (key ASC)");
    }

    public override void Down()
    {
    }
}
