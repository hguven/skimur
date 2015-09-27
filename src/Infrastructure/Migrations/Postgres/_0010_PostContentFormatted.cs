﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Data;
using Infrastructure.Postgres.Migrations;
using ServiceStack.OrmLite.Dapper;

namespace Migrations.Postgres
{
    public class _0010_PostContentFormatted : Migration
    {
        public _0010_PostContentFormatted() : base(MigrationType.Schema, 10)
        {

        }


        public override void Execute(IDbConnectionProvider conn)
        {
            conn.Perform(x =>
            {
                x.Execute("ALTER TABLE posts ADD COLUMN content_formatted text NULL;");
            });
        }

        public override string GetDescription()
        {
            return "Add a pre-renderer content field to the post table.";
        }
    }
}
