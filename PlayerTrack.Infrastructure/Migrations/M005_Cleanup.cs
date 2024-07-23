using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Helpers;
using FluentDapperLite.Extension;
using FluentMigrator;
using Newtonsoft.Json;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Repositories.Migrations;

[Migration(20240721120000)]
public class M005_Cleanup: Migration
{

    public override void Up()
    {
        this.Delete.Table("lodestone_lookups");
    }
    
    public override void Down()
    {
    }
}