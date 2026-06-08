using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace CareerHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobListingAndApplicationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_listings_CompanyId",
                table: "job_listings");

            migrationBuilder.RenameIndex(
                name: "IX_applications_JobListingId",
                table: "applications",
                newName: "ix_applications_joblistingid");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "job_listings",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Description" });

            migrationBuilder.CreateIndex(
                name: "ix_job_listings_companyid_isactive",
                table: "job_listings",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_job_listings_isactive_closingdate",
                table: "job_listings",
                columns: new[] { "IsActive", "ClosingDate" });

            migrationBuilder.CreateIndex(
                name: "ix_job_listings_search_vector",
                table: "job_listings",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_applications_applicantid_joblistingid",
                table: "applications",
                columns: new[] { "ApplicantId", "JobListingId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_job_listings_companyid_isactive",
                table: "job_listings");

            migrationBuilder.DropIndex(
                name: "ix_job_listings_isactive_closingdate",
                table: "job_listings");

            migrationBuilder.DropIndex(
                name: "ix_job_listings_search_vector",
                table: "job_listings");

            migrationBuilder.DropIndex(
                name: "ix_applications_applicantid_joblistingid",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "job_listings");

            migrationBuilder.RenameIndex(
                name: "ix_applications_joblistingid",
                table: "applications",
                newName: "IX_applications_JobListingId");

            migrationBuilder.CreateIndex(
                name: "IX_job_listings_CompanyId",
                table: "job_listings",
                column: "CompanyId");
        }
    }
}
