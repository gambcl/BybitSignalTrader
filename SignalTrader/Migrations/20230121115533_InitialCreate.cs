using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SignalTrader.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "signaltrader");

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "signaltrader",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "varchar(500)", maxLength: 100, nullable: false),
                    Comment = table.Column<string>(type: "varchar(4000)", maxLength: 1000, nullable: true),
                    Exchange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuoteAsset = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    ApiKey = table.Column<string>(type: "varchar(2000)", maxLength: 500, nullable: true),
                    ApiSecret = table.Column<string>(type: "varchar(2000)", maxLength: 500, nullable: true),
                    ApiPassphrase = table.Column<string>(type: "varchar(2000)", maxLength: 500, nullable: true),
                    CreatedUtcMillis = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedUtcMillis = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountType",
                schema: "signaltrader",
                table: "Accounts",
                column: "AccountType");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Exchange",
                schema: "signaltrader",
                table: "Accounts",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_QuoteAsset",
                schema: "signaltrader",
                table: "Accounts",
                column: "QuoteAsset");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "signaltrader");
        }
    }
}
