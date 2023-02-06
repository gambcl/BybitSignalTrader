using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SignalTrader.Migrations
{
    public partial class AddSignalsPositionsOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExchangeAccountId",
                schema: "signaltrader",
                table: "Accounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeType",
                schema: "signaltrader",
                table: "Accounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Positions",
                schema: "signaltrader",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Exchange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuoteAsset = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseAsset = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    LeverageMultiplier = table.Column<decimal>(type: "numeric", nullable: true),
                    LeverageType = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnrealisedPnl = table.Column<decimal>(type: "numeric", nullable: false),
                    UnrealisedPnlPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    RealisedPnl = table.Column<decimal>(type: "numeric", nullable: false),
                    RealisedPnlPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    StopLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    LiquidationPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedUtcMillis = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedUtcMillis = table.Column<long>(type: "bigint", nullable: false),
                    CompletedUtcMillis = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "signaltrader",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Signals",
                schema: "signaltrader",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StrategyName = table.Column<string>(type: "text", nullable: false),
                    SignalName = table.Column<string>(type: "text", nullable: false),
                    QuoteAsset = table.Column<string>(type: "text", nullable: false),
                    BaseAsset = table.Column<string>(type: "text", nullable: false),
                    SignalTimeUtcMillis = table.Column<long>(type: "bigint", nullable: false),
                    Exchange = table.Column<string>(type: "text", nullable: false),
                    Ticker = table.Column<string>(type: "text", nullable: false),
                    Interval = table.Column<string>(type: "text", nullable: false),
                    BarTimeUtcMillis = table.Column<long>(type: "bigint", nullable: false),
                    Open = table.Column<decimal>(type: "numeric", nullable: false),
                    High = table.Column<decimal>(type: "numeric", nullable: false),
                    Low = table.Column<decimal>(type: "numeric", nullable: false),
                    Close = table.Column<decimal>(type: "numeric", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric", nullable: false),
                    LongEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ShortEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtcMillis = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "signaltrader",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Exchange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuoteAsset = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseAsset = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExchangeOrderId = table.Column<string>(type: "text", nullable: true),
                    Side = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityFilled = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TakeProfit = table.Column<decimal>(type: "numeric", nullable: true),
                    StopLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    ReduceOnly = table.Column<bool>(type: "boolean", nullable: true),
                    PositionId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedUtcMillis = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedUtcMillis = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "signaltrader",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "signaltrader",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Exchange_ExchangeAccountId",
                schema: "signaltrader",
                table: "Accounts",
                columns: new[] { "Exchange", "ExchangeAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ExchangeAccountId",
                schema: "signaltrader",
                table: "Accounts",
                column: "ExchangeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ExchangeType",
                schema: "signaltrader",
                table: "Accounts",
                column: "ExchangeType");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AccountId",
                schema: "signaltrader",
                table: "Orders",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BaseAsset",
                schema: "signaltrader",
                table: "Orders",
                column: "BaseAsset");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Exchange",
                schema: "signaltrader",
                table: "Orders",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExchangeOrderId",
                schema: "signaltrader",
                table: "Orders",
                column: "ExchangeOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PositionId",
                schema: "signaltrader",
                table: "Orders",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_QuoteAsset",
                schema: "signaltrader",
                table: "Orders",
                column: "QuoteAsset");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Side",
                schema: "signaltrader",
                table: "Orders",
                column: "Side");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "signaltrader",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_AccountId",
                schema: "signaltrader",
                table: "Positions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_BaseAsset",
                schema: "signaltrader",
                table: "Positions",
                column: "BaseAsset");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Direction",
                schema: "signaltrader",
                table: "Positions",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Exchange",
                schema: "signaltrader",
                table: "Positions",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_QuoteAsset",
                schema: "signaltrader",
                table: "Positions",
                column: "QuoteAsset");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Status",
                schema: "signaltrader",
                table: "Positions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_BarTimeUtcMillis",
                schema: "signaltrader",
                table: "Signals",
                column: "BarTimeUtcMillis");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_BaseAsset",
                schema: "signaltrader",
                table: "Signals",
                column: "BaseAsset");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_Exchange",
                schema: "signaltrader",
                table: "Signals",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_Interval",
                schema: "signaltrader",
                table: "Signals",
                column: "Interval");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_QuoteAsset",
                schema: "signaltrader",
                table: "Signals",
                column: "QuoteAsset");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_SignalName",
                schema: "signaltrader",
                table: "Signals",
                column: "SignalName");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_SignalTimeUtcMillis",
                schema: "signaltrader",
                table: "Signals",
                column: "SignalTimeUtcMillis");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_StrategyName",
                schema: "signaltrader",
                table: "Signals",
                column: "StrategyName");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_Ticker",
                schema: "signaltrader",
                table: "Signals",
                column: "Ticker");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders",
                schema: "signaltrader");

            migrationBuilder.DropTable(
                name: "Signals",
                schema: "signaltrader");

            migrationBuilder.DropTable(
                name: "Positions",
                schema: "signaltrader");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Exchange_ExchangeAccountId",
                schema: "signaltrader",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_ExchangeAccountId",
                schema: "signaltrader",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_ExchangeType",
                schema: "signaltrader",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ExchangeAccountId",
                schema: "signaltrader",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ExchangeType",
                schema: "signaltrader",
                table: "Accounts");
        }
    }
}
