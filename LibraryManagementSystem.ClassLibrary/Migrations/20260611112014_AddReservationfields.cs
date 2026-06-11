using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.ClassLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowRecords_UserTokens_UserTokenId",
                table: "BorrowRecords");

            migrationBuilder.DropTable(
                name: "TokenPayments");

            migrationBuilder.DropTable(
                name: "TokenRefunds");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_UserTokenId",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "IsTokenBorrow",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "UserTokenId",
                table: "BorrowRecords");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentCompleted",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentRequired",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "Reservations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaymentCompleted",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentRequired",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Reservations");

            migrationBuilder.AddColumn<bool>(
                name: "IsTokenBorrow",
                table: "BorrowRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UserTokenId",
                table: "BorrowRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AvailableTokens = table.Column<int>(type: "int", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalBorrowCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserTokenId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScreenshotPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenPayments_UserTokens_UserTokenId",
                        column: x => x.UserTokenId,
                        principalTable: "UserTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenRefunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorrowRecordId = table.Column<int>(type: "int", nullable: false),
                    UserTokenId = table.Column<int>(type: "int", nullable: false),
                    BookCondition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenRefunds_BorrowRecords_BorrowRecordId",
                        column: x => x.BorrowRecordId,
                        principalTable: "BorrowRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TokenRefunds_UserTokens_UserTokenId",
                        column: x => x.UserTokenId,
                        principalTable: "UserTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_UserTokenId",
                table: "BorrowRecords",
                column: "UserTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPayments_UserTokenId",
                table: "TokenPayments",
                column: "UserTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenRefunds_BorrowRecordId",
                table: "TokenRefunds",
                column: "BorrowRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenRefunds_UserTokenId",
                table: "TokenRefunds",
                column: "UserTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_UserId",
                table: "UserTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowRecords_UserTokens_UserTokenId",
                table: "BorrowRecords",
                column: "UserTokenId",
                principalTable: "UserTokens",
                principalColumn: "Id");
        }
    }
}
