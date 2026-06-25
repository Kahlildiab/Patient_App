using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalCollegeManagementSystem_AAU.Migrations
{
    /// <summary>
    /// يضيف عمود BpeJson فقط إلى جدول DentalChartSessions.
    /// لا ينشئ أو يحذف أي جدول آخر موجود في قاعدة البيانات.
    /// </summary>
    public partial class AddBpeJsonToDentalChartSession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * نستخدم SQL مشروطًا لتجنب الخطأ في حال كان العمود
             * موجودًا أصلًا داخل قاعدة البيانات.
             *
             * default = {}
             * حتى ينجح إضافة العمود إذا كان الجدول يحتوي سجلات قديمة.
             */
            migrationBuilder.Sql(
                @"
                IF COL_LENGTH('dbo.DentalChartSessions', 'BpeJson') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[DentalChartSessions]
                    ADD [BpeJson] nvarchar(500) NOT NULL
                        CONSTRAINT [DF_DentalChartSessions_BpeJson]
                        DEFAULT N'{}';
                END
                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
             * حذف الـ Default Constraint أولًا، ثم حذف العمود فقط.
             * لا يتم حذف DentalChartSessions أو أي جدول آخر.
             */
            migrationBuilder.Sql(
                @"
                IF COL_LENGTH('dbo.DentalChartSessions', 'BpeJson') IS NOT NULL
                BEGIN
                    DECLARE @ConstraintName nvarchar(128);

                    SELECT @ConstraintName = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c
                        ON c.default_object_id = dc.object_id
                    INNER JOIN sys.tables t
                        ON t.object_id = c.object_id
                    INNER JOIN sys.schemas s
                        ON s.schema_id = t.schema_id
                    WHERE s.name = N'dbo'
                      AND t.name = N'DentalChartSessions'
                      AND c.name = N'BpeJson';

                    IF @ConstraintName IS NOT NULL
                    BEGIN
                        EXEC(
                            N'ALTER TABLE [dbo].[DentalChartSessions] ' +
                            N'DROP CONSTRAINT [' +
                            REPLACE(@ConstraintName, N']', N']]') +
                            N']'
                        );
                    END

                    ALTER TABLE [dbo].[DentalChartSessions]
                    DROP COLUMN [BpeJson];
                END
                ");
        }
    }
}
