using MedicalReportSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace MedicalReportSystem.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<T_TEST_REC> T_TEST_RECS { get; set; }
        public DbSet<T_testr_res_indicate> TESTR_RES_INDICATES { get; set; }
        public DbSet<T_MICROBE_BACTERIA_RES> MICROBE_BACTERIA_RES { get; set; }
        public DbSet<T_MICROBE_SUSCEPT_RES> MICROBE_SUSCEPT_REs { get; set; }
        public DbSet<T_CHECK_REC> t_CHECK_RECs { get; set; }
        public DbSet<ReportRecognition> RecognitionRecords { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 指定schema
            // 配置 T_TEST_REC
            modelBuilder.Entity<T_TEST_REC>().ToTable("t_test_rec", "sharedata");
            // 配置 T_TESTR_RES_INDICATE
            modelBuilder.Entity<T_testr_res_indicate>()
                .ToTable("t_testr_res_indicate", "sharedata")  // 指定表名和schema
                .HasKey(e => e.TestReportNo);  // 指定主键
            // 配置 T_MICROBE_BACTERIA_RES
            modelBuilder.Entity<T_MICROBE_BACTERIA_RES>()
                .ToTable("t_microbe_bacteria_res", "sharedata") // 指定表名和schema
                .HasKey(e => e.TestReportNo);

            // 配置 T_MICROBE_SUSCEPT_RES
            modelBuilder.Entity<T_MICROBE_SUSCEPT_RES>()
                .ToTable("t_microbe_suscept_res", "sharedata") // 指定表名和schema
                .HasKey(e => e.TestReportNo);

            // 配置 T_MICROBE_SUSCEPT_RES
            modelBuilder.Entity<T_CHECK_REC>()
                .ToTable("t_check_rec", "sharedata") // 指定表名和schema
                .HasKey(e => e.CheckReportNo);

            // 配置RecognitionRecord实体
            modelBuilder.Entity<RecognitionRecord>(entity =>
            {
                // 设置复合主键（如果需要）
                // entity.HasKey(e => new { e.REPORT_ID, e.PATIENT_ID });

                // 设置索引
                entity.HasIndex(e => e.REPORT_ID).IsUnique(false);
                entity.HasIndex(e => e.PATIENT_ID);
                entity.HasIndex(e => e.RECOGNITION_STATUS);
                entity.HasIndex(e => e.UPDATE_TIME);

                // 配置字段属性
                entity.Property(e => e.RECOGNITION_STATUS)
                    .HasDefaultValue(2); // 默认状态为未处理

                entity.Property(e => e.UPDATE_TIME)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                // 配置关系（根据需要）
                // entity.HasOne(e => e.Patient)
                //     .WithMany(p => p.RecognitionRecords)
                //     .HasForeignKey(e => e.PATIENT_ID);
            });
            //modelBuilder.Entity<T_testr_res_indicate>()
            //    .HasOne(e => e.TestRecord)              // 明细表有一个主表记录
            //    .WithMany(e => e.Indicators)            // 主表对应多个明细记录
            //    .HasForeignKey(e => e.TestReportNo)     // 外键字段
            //    .HasPrincipalKey(e => e.TestReportNo)  // 主表关联字段（需唯一）
            //    .OnDelete(DeleteBehavior.Cascade);      // 主表删除时级联删除明细
            //// 配置 T_TESTR_RES_INDICATE 与 T_TEST_REC 的关系（原配置保留）
            //modelBuilder.Entity<T_testr_res_indicate>()
            //    .HasOne(e => e.TestRecord)
            //    .WithMany()
            //    .HasForeignKey(e => e.TestReportNo)
            //    .HasPrincipalKey(e => e.TestReportNo);

            //// 新增：配置 T_MICROBE_BACTERIA_RES 与 T_TEST_REC 的关系
            //modelBuilder.Entity<T_MICROBE_BACTERIA_RES>()
            //    .HasOne(e => e.TestRecord)              // 导航属性
            //    .WithMany()                             // T_TEST_REC 无反向集合时使用
            //    .HasForeignKey(e => e.TestReportNo)     // 外键字段
            //    .HasPrincipalKey(e => e.TestReportNo)   // 主表关联字段
            //    .OnDelete(DeleteBehavior.Cascade);      // 级联删除（可选）
        }
    }
}
