using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DateDemo.Migrations
{
    /// <inheritdoc/>
    public partial class InitialExample : Migration
    {
        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "ingres");
            migrationBuilder.CreateTable(name: "date_samples", schema: "ingres", columns: table => new { id = table.Column<int>(type: "integer", nullable: false), label = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false), native_value = table.Column<string>(type: "ingresdate", nullable: false), nullable_value = table.Column<string>(type: "ingresdate", nullable: true), lead_time = table.Column<string>(type: "ingresdate", nullable: false), required_date = table.Column<string>(type: "ingresdate", nullable: false, defaultValueSql: "''") }, constraints: table =>
            {
                table.PrimaryKey("PK_date_samples", x => x.id);
            });
        }

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "date_samples", schema: "ingres");
        }
    }
}
