using Microsoft.EntityFrameworkCore;
using SQE_Practice.Storage.Entities;

namespace SQE_Practice.Storage
{
    public class SqeDbContext :DbContext
    {
        public SqeDbContext(DbContextOptions<SqeDbContext> options) : base(options)
        {
        }

        public DbSet<TelemetryEventEntity> Events =>
        Set<TelemetryEventEntity>();

        public DbSet<MetricEntity> Metrics =>
        Set<MetricEntity>();

        public DbSet<AlertEntity> Alerts =>
        Set<AlertEntity>();

        public DbSet<RequestLogEntity> RequestLogs =>
        Set<RequestLogEntity>();

        protected override void OnModelCreating(
        ModelBuilder modelBuilder)
        {
            ConfigureEvents(modelBuilder);
            ConfigureMetrics(modelBuilder);
            ConfigureAlerts(modelBuilder);
            ConfigureRequestLogs(modelBuilder);
        }

        private static void ConfigureEvents(
        ModelBuilder modelBuilder)
        {
            var entity =
            modelBuilder.Entity<TelemetryEventEntity>();

            entity.ToTable("Events");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.EventName)
            .HasMaxLength(200)
            .IsRequired();

            entity.Property(x => x.Service)
            .HasMaxLength(200)
            .IsRequired();

            entity.Property(x => x.Region)
            .HasMaxLength(100)
            .IsRequired();

            entity.Property(x => x.PropertiesJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

            entity.HasIndex(x => x.EventId)
            .IsUnique();

            entity.HasIndex(x => new
            {
                x.EventName,
                x.Timestamp
            });

            entity.HasIndex(x => new
            {
                x.Service,
                x.Region
            });
        }

        private static void ConfigureMetrics(
        ModelBuilder modelBuilder)
        {
            var entity =
            modelBuilder.Entity<MetricEntity>();

            entity.ToTable("Metrics");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

            entity.Property(x => x.DimensionsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

            entity.HasIndex(x => new
            {
                x.Name,
                x.Timestamp
            });
        }

        private static void ConfigureAlerts(
        ModelBuilder modelBuilder)
        {
            var entity =
            modelBuilder.Entity<AlertEntity>();

            entity.ToTable("Alerts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.QueryName)
            .HasMaxLength(200)
            .IsRequired();

            entity.Property(x => x.Message)
            .HasMaxLength(1000)
            .IsRequired();

            entity.Property(x => x.DimensionsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

            entity.HasIndex(x => new
            {
                x.QueryName,
                x.Timestamp
            });
        }

        private static void ConfigureRequestLogs(
        ModelBuilder modelBuilder)
        {
            var entity =
            modelBuilder.Entity<RequestLogEntity>();

            entity.ToTable("RequestLogs");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.RequestId)
            .IsUnique();

            entity.Property(x => x.Method)
            .HasMaxLength(10)
            .IsRequired();

            entity.Property(x => x.Path)
            .HasMaxLength(500)
            .IsRequired();

            entity.Property(x => x.QueryString)
            .HasMaxLength(2000);

            entity.Property(x => x.Severity)
            .HasMaxLength(20)
            .IsRequired();

            entity.Property(x => x.ClientIp)
            .HasMaxLength(50);

            entity.Property(x => x.UserAgent)
            .HasMaxLength(500);

            entity.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

            entity.Property(x => x.ErrorType)
            .HasMaxLength(500);

            entity.Property(x => x.RequestBody)
            .HasColumnType("nvarchar(max)");

            entity.Property(x => x.ResponseBody)
            .HasColumnType("nvarchar(max)");

            // Query logs by severity (e.g. only Errors)
            entity.HasIndex(x => new
            {
                x.Severity,
                x.Timestamp
            });

            // Query logs by endpoint path
            entity.HasIndex(x => new
            {
                x.Path,
                x.Timestamp
            });
        }
    }
}
