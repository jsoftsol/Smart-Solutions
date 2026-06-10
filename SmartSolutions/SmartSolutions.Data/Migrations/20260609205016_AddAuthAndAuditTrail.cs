using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutions.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthAndAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "PrintOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecordedById",
                table: "PrintOrderPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "HaierJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecordedById",
                table: "HaierJobPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PinHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintOrders_CreatedById",
                table: "PrintOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PrintOrderPayments_RecordedById",
                table: "PrintOrderPayments",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_HaierJobs_CreatedById",
                table: "HaierJobs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_HaierJobPayments_RecordedById",
                table: "HaierJobPayments",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CreatedById",
                table: "Expenses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedById",
                table: "Customers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_AppUsers_CreatedById",
                table: "Customers",
                column: "CreatedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_AppUsers_CreatedById",
                table: "Expenses",
                column: "CreatedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HaierJobPayments_AppUsers_RecordedById",
                table: "HaierJobPayments",
                column: "RecordedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HaierJobs_AppUsers_CreatedById",
                table: "HaierJobs",
                column: "CreatedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrintOrderPayments_AppUsers_RecordedById",
                table: "PrintOrderPayments",
                column: "RecordedById",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrintOrders_AppUsers_CreatedById",
                table: "PrintOrders",
                column: "CreatedById",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_AppUsers_CreatedById",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_AppUsers_CreatedById",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_HaierJobPayments_AppUsers_RecordedById",
                table: "HaierJobPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_HaierJobs_AppUsers_CreatedById",
                table: "HaierJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_PrintOrderPayments_AppUsers_RecordedById",
                table: "PrintOrderPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_PrintOrders_AppUsers_CreatedById",
                table: "PrintOrders");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_PrintOrders_CreatedById",
                table: "PrintOrders");

            migrationBuilder.DropIndex(
                name: "IX_PrintOrderPayments_RecordedById",
                table: "PrintOrderPayments");

            migrationBuilder.DropIndex(
                name: "IX_HaierJobs_CreatedById",
                table: "HaierJobs");

            migrationBuilder.DropIndex(
                name: "IX_HaierJobPayments_RecordedById",
                table: "HaierJobPayments");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CreatedById",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CreatedById",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "PrintOrders");

            migrationBuilder.DropColumn(
                name: "RecordedById",
                table: "PrintOrderPayments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "HaierJobs");

            migrationBuilder.DropColumn(
                name: "RecordedById",
                table: "HaierJobPayments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Customers");
        }
    }
}
