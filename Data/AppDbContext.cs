using System;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MedicalHistory> MedicalHistories { get; set; }
        public DbSet<Condition> Conditions { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<DentalHistory> DentalHistories { get; set; }
        public DbSet<SocialHistory> SocialHistories { get; set; }
        public DbSet<ExtraoralExam> ExtraoralExams { get; set; }
        public DbSet<IntraoralExam> IntraoralExams { get; set; }
        public DbSet<ExtraoralExamPhoto> ExtraoralExamPhotos { get; set; }
        public DbSet<IntraoralExamPhoto> IntraoralExamPhotos { get; set; }

        public DbSet<TreatmentProcedure>
            TreatmentProcedures
        { get; set; }

        public DbSet<TreatmentPlanDiagnosis>
            TreatmentPlanDiagnoses
        { get; set; }

        public DbSet<Visit> Visits { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Radiograph> Radiographs { get; set; }
        public DbSet<PatientPhoto> PatientPhotos { get; set; }

        /*
         * نفس جدول Competency الحالي سيحتوي على:
         *
         * 1) القوالب الأصلية:
         *    PatientID = null
         *    StudentUserID = null
         *    TemplateCompetencyID = null
         *
         * 2) السجلات القديمة الخاصة بالمريض:
         *    PatientID = رقم المريض
         *
         * 3) السجلات الجديدة الخاصة بالطالب:
         *    StudentUserID = رقم الطالب
         *    TemplateCompetencyID = رقم القالب
         */
        public DbSet<Competency> Competency { get; set; }

        public DbSet<Order> Orders { get; set; }

        // public DbSet<ConsentOrder> ConsentOrders { get; set; }
        // public DbSet<ReferralOrder> ReferralOrders { get; set; }
        // public DbSet<MedicationOrder> MedicationOrders { get; set; }
        // public DbSet<XRayOrder> XRayOrders { get; set; }
        // public DbSet<DischargeOrder> DischargeOrders { get; set; }

        public DbSet<UserType> UserTypes { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }

        public DbSet<AllocatedStudent>
            AllocatedStudents
        { get; set; }

        // ── Dental Chart (علائقي بدل JSON) ──────────────────────
        public DbSet<DentalChartSession>
            DentalChartSessions
        { get; set; }

        public DbSet<DentalToothData>
            DentalToothData
        { get; set; }

        public DbSet<PatientStatusHistory>
            PatientStatusHistories
        { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // Student Note Approval Workflow
            // =====================================================

            modelBuilder.Entity<Note>()
                .Property(note => note.ApprovalStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Approved");

            modelBuilder.Entity<Note>()
                .Property(note => note.CreatedByRole)
                .HasMaxLength(50)
                .HasDefaultValue("Unknown");

            modelBuilder.Entity<Note>()
                .HasOne(note => note.Visit)
                .WithMany()
                .HasForeignKey(note => note.VisitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Note>()
                .HasIndex(
                    note =>
                        new
                        {
                            note.VisitId,
                            note.ApprovalStatus
                        }
                );

            // =====================================================
            // Dental Chart Indexes
            // =====================================================

            modelBuilder.Entity<DentalChartSession>()
                .HasIndex(
                    session =>
                        session.PatientId
                );

            modelBuilder.Entity<DentalToothData>()
                .HasIndex(
                    tooth =>
                        new
                        {
                            tooth.SessionId,
                            tooth.ToothId,
                            tooth.AreaName
                        }
                )
                .IsUnique();

            // =====================================================
            // Competency - Student Relationship
            // =====================================================

            /*
             * ربط سجل Competency الخاص بالطالب
             * مع جدول Users.
             *
             * Restrict يمنع حذف الطالب تلقائياً إذا كان لديه
             * Competency records.
             */
            modelBuilder.Entity<Competency>()
                .HasOne(
                    competency =>
                        competency.Student
                )
                .WithMany()
                .HasForeignKey(
                    competency =>
                        competency.StudentUserID
                )
                .OnDelete(
                    DeleteBehavior.Restrict
                );

            /*
             * علاقة ذاتية:
             * سجل الطالب يشير إلى قالب Competency الأصلي.
             */
            modelBuilder.Entity<Competency>()
                .HasOne(
                    competency =>
                        competency.TemplateCompetency
                )
                .WithMany()
                .HasForeignKey(
                    competency =>
                        competency.TemplateCompetencyID
                )
                .OnDelete(
                    DeleteBehavior.Restrict
                );

            /*
             * يمنع تكرار نفس Competency لنفس الطالب.
             *
             * الفلتر مهم لأن القوالب القديمة وسجلات المرضى
             * تحتوي على قيم null في هذين العمودين.
             */
            modelBuilder.Entity<Competency>()
                .HasIndex(
                    competency =>
                        new
                        {
                            competency.StudentUserID,
                            competency.TemplateCompetencyID
                        }
                )
                .IsUnique()
                .HasFilter(
                    "[StudentUserID] IS NOT NULL "
                    + "AND [TemplateCompetencyID] IS NOT NULL"
                );

            // =====================================================
            // Seed Data - Competencies
            // =====================================================

            modelBuilder.Entity<Competency>().HasData(
                new Competency
                {
                    CompetencyID = 1,
                    CategoryName = "Clinical Examination",
                    CategoryOrder = 1,
                    ItemName = "Medical History Assessment",
                    ItemOrder = 1
                },
                new Competency
                {
                    CompetencyID = 2,
                    CategoryName = "Clinical Examination",
                    CategoryOrder = 1,
                    ItemName = "Extraoral Examination",
                    ItemOrder = 2
                },
                new Competency
                {
                    CompetencyID = 3,
                    CategoryName = "Clinical Examination",
                    CategoryOrder = 1,
                    ItemName = "Intraoral Examination",
                    ItemOrder = 3
                },
                new Competency
                {
                    CompetencyID = 4,
                    CategoryName = "Clinical Examination",
                    CategoryOrder = 1,
                    ItemName = "Periodontal Assessment",
                    ItemOrder = 4
                },
                new Competency
                {
                    CompetencyID = 5,
                    CategoryName = "Diagnostic Procedures",
                    CategoryOrder = 2,
                    ItemName = "Radiographic Interpretation",
                    ItemOrder = 1
                },
                new Competency
                {
                    CompetencyID = 6,
                    CategoryName = "Diagnostic Procedures",
                    CategoryOrder = 2,
                    ItemName = "Treatment Planning",
                    ItemOrder = 2
                },
                new Competency
                {
                    CompetencyID = 7,
                    CategoryName = "Diagnostic Procedures",
                    CategoryOrder = 2,
                    ItemName = "Case Presentation",
                    ItemOrder = 3
                },
                new Competency
                {
                    CompetencyID = 8,
                    CategoryName = "Restorative Dentistry",
                    CategoryOrder = 3,
                    ItemName = "Cavity Preparation",
                    ItemOrder = 1
                },
                new Competency
                {
                    CompetencyID = 9,
                    CategoryName = "Restorative Dentistry",
                    CategoryOrder = 3,
                    ItemName = "Direct Restoration Placement",
                    ItemOrder = 2
                },
                new Competency
                {
                    CompetencyID = 10,
                    CategoryName = "Restorative Dentistry",
                    CategoryOrder = 3,
                    ItemName = "Finishing and Polishing",
                    ItemOrder = 3
                },
                new Competency
                {
                    CompetencyID = 11,
                    CategoryName = "Periodontics",
                    CategoryOrder = 4,
                    ItemName = "Scaling and Root Planing",
                    ItemOrder = 1
                },
                new Competency
                {
                    CompetencyID = 12,
                    CategoryName = "Periodontics",
                    CategoryOrder = 4,
                    ItemName = "Oral Hygiene Instructions",
                    ItemOrder = 2
                },
                new Competency
                {
                    CompetencyID = 13,
                    CategoryName = "Professional Skills",
                    CategoryOrder = 5,
                    ItemName = "Patient Communication",
                    ItemOrder = 1
                },
                new Competency
                {
                    CompetencyID = 14,
                    CategoryName = "Professional Skills",
                    CategoryOrder = 5,
                    ItemName = "Infection Control",
                    ItemOrder = 2
                },
                new Competency
                {
                    CompetencyID = 15,
                    CategoryName = "Professional Skills",
                    CategoryOrder = 5,
                    ItemName = "Documentation",
                    ItemOrder = 3
                },
                new Competency
                {
                    CompetencyID = 16,
                    CategoryName = "Professional Skills",
                    CategoryOrder = 5,
                    ItemName = "Time Management",
                    ItemOrder = 4
                }
            );

            // =====================================================
            // Seed Data - Statuses
            // =====================================================

            modelBuilder.Entity<Status>().HasData(
                new Status
                {
                    StatusID = 1,
                    StatusName = "Active",
                    StatusDescription =
                        "Patient just registered",
                    CreatedDate =
                        new DateTime(2024, 1, 1)
                },
                new Status
                {
                    StatusID = 2,
                    StatusName = "Accepted",
                    StatusDescription =
                        "Patient accepted",
                    CreatedDate =
                        new DateTime(2024, 1, 1)
                },
                new Status
                {
                    StatusID = 3,
                    StatusName = "Pending",
                    StatusDescription =
                        "Appointment taken",
                    CreatedDate =
                        new DateTime(2024, 1, 1)
                },
                new Status
                {
                    StatusID = 4,
                    StatusName = "In Treatment",
                    StatusDescription =
                        "Treatment started",
                    CreatedDate =
                        new DateTime(2024, 1, 1)
                },
                new Status
                {
                    StatusID = 5,
                    StatusName = "Completed",
                    StatusDescription =
                        "Treatment completed",
                    CreatedDate =
                        new DateTime(2024, 1, 1)
                },
                new Status
                {
                    StatusID = 6,
                    StatusName = "Rejected",
                    StatusDescription =
                        "Patient rejected",
                    CreatedDate =
                        new DateTime(2024, 1, 1)
                }
            );

            // =====================================================
            // Allocated Student
            // =====================================================

            modelBuilder.Entity<AllocatedStudent>()
                .HasIndex(
                    allocation =>
                        new
                        {
                            allocation.PatientID,
                            allocation.AppUserId
                        }
                )
                .IsUnique();
        }
    }
}
