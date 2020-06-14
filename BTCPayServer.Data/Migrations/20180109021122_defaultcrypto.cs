using BTCPayServer.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20180109021122_defaultbitcoin")]
    public partial class defaultbitcoin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            int? maxLength = this.IsMySql(migrationBuilder.ActiveProvider) ? (int?)255 : null;
            migrationBuilder.AddColumn<string>(
                name: "Defaultbitcoin",
                table: "Stores",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Defaultbitcoin",
                table: "Stores");
        }
    }
}
