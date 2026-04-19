using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bielu.Ef.Examples.Api.Postgres.Versioning.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contents",
                columns: table => new
                {
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveAt = table.Column<long>(type: "bigint", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contents", x => x.VersionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contents_EntityId_EffectiveAt",
                table: "Contents",
                columns: new[] { "EntityId", "EffectiveAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Contents_EntityId_EffectiveAt_VersionId",
                table: "Contents",
                columns: new[] { "EntityId", "EffectiveAt", "VersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contents_EntityId_VersionNumber",
                table: "Contents",
                columns: new[] { "EntityId", "VersionNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contents");
        }
    }
}
