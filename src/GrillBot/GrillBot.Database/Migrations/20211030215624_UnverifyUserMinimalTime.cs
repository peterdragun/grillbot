﻿using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

namespace GrillBot.Database.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class UnverifyUserMinimalTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flags",
                table: "Channels");

            migrationBuilder.AddColumn<string>(
                name: "UnverifyMinimalTime",
                table: "Users",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnverifyMinimalTime",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "Flags",
                table: "Channels",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
