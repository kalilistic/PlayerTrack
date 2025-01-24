using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentDapperLite.Extension;
using FluentMigrator;
using Newtonsoft.Json;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Repositories.Migrations;

[Migration(20240723120000)]
public class M006_FirstSeen: FluentMigrator.Migration
{

    public override void Up()
    {
        Alter.Table("players")
            .AddColumn("first_seen").AsInt64().NotNullable().WithDefaultValue(0);
        Execute.Sql(@"
            UPDATE players
            SET first_seen = created
            WHERE seen_count > 0;
        ");
    }

    public override void Down()
    {
    }
}
