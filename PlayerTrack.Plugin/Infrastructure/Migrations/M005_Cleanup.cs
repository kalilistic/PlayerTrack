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

[Migration(20240721120000)]
public class M005_Cleanup: FluentMigrator.Migration
{

    public override void Up()
    {
        Delete.Table("lodestone_lookups");
    }

    public override void Down()
    {
    }
}
