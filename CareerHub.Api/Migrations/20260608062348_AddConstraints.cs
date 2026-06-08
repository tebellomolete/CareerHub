using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_joblistings_expiresaftercreated",
                table: "job_listings",
                sql: "\"ClosingDate\" > \"PostedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_joblistings_salarymax",
                table: "job_listings",
                sql: "\"SalaryMax\" > \"SalaryMin\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_joblistings_salarymin",
                table: "job_listings",
                sql: "\"SalaryMin\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_applications_submitted_not_future",
                table: "applications",
                sql: "\"SubmittedAt\" <= now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_joblistings_expiresaftercreated",
                table: "job_listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_joblistings_salarymax",
                table: "job_listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_joblistings_salarymin",
                table: "job_listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_applications_submitted_not_future",
                table: "applications");
        }
    }
}
