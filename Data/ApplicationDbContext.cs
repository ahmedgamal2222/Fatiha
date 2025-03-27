using Fatiha__app.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fatiha__app.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AlQerat> alQerats { get; set; }
        public DbSet<ApplicationUser> applicationUsers { get; set; }
        public DbSet<Certificate> certificates { get; set; }
        public DbSet<AuthorizedUsers> AuthorizedUsers { get; set; }
        public DbSet<PointsLogs> PointsLogs { get; set; }
        public DbSet<FatihaExam> fatihaExams { get; set; }
        public DbSet<FatihaRequest> fatihaRequests { get; set; }
        public DbSet<FatihaRequestComment> fatihaRequestsComment { get; set; }
        public DbSet<languageS> languages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<languageS>().HasData(
                new languageS { Id = 1, LanguageName = EnumCommon.Languagelist.العربية }, // Arabic
                new languageS { Id = 2, LanguageName = EnumCommon.Languagelist.English }, // English
                new languageS { Id = 3, LanguageName = EnumCommon.Languagelist.普通话 }, // Mandarin
                new languageS { Id = 4, LanguageName = EnumCommon.Languagelist.Español }, // Spanish
                new languageS { Id = 5, LanguageName = EnumCommon.Languagelist.हिंदी }, // Hindi
                new languageS { Id = 6, LanguageName = EnumCommon.Languagelist.Français }, // French
                new languageS { Id = 7, LanguageName = EnumCommon.Languagelist.Русский }, // Russian
                new languageS { Id = 8, LanguageName = EnumCommon.Languagelist.বাংলা }, // Bengali
                new languageS { Id = 9, LanguageName = EnumCommon.Languagelist.Português }, // Portuguese
                new languageS { Id = 10, LanguageName = EnumCommon.Languagelist.اُردُو }, // Urdu
                new languageS { Id = 11, LanguageName = EnumCommon.Languagelist.BahasaIndonesia } // Indonesian

            );
            modelBuilder.Entity<AuthorizedUsers>()
       .Property(u => u.ProfileImg)
       .HasColumnType("nvarchar(MAX)");
            modelBuilder.Entity<Certificate>()
         .HasOne(c => c.AuthorizedByUser)
         .WithMany()
         .HasForeignKey(c => c.AuthorizedByUserId)
         .OnDelete(DeleteBehavior.NoAction);

            // علاقة Certificate مع ApplicationUser (يمكنك تركها Cascade إذا كنت تريد حذف الشهادة مع حذف المستخدم)
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.ApplicationUser)
                .WithMany()
                .HasForeignKey(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // علاقة Certificate مع FatihaRequest (تغيير DeleteBehavior لتكون NoAction لتجنب مسارات الحذف المتعددة)
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.FatihaRequest)
                .WithMany()
                .HasForeignKey(c => c.FatihaRequestId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<FatihaRequestComment>()
     .HasOne(c => c.FatihaRequest)
     .WithMany(r => r.Comments) // ✅ تأكد من وجود "Comments" في `FatihaRequest`
     .HasForeignKey(c => c.FatihaRequestId)
     .OnDelete(DeleteBehavior.Cascade); // ✅ تفعيل الحذف التتابعي

            modelBuilder.Entity<PointsLogs>()
           .HasOne(p => p.FatihaRequest)
           .WithMany(r => r.PointsLogs) // أضف هذه الإشارة إلى التجميع في FatihaRequest
           .HasForeignKey(p => p.FatihaRequestId)
           .OnDelete(DeleteBehavior.NoAction);



            base.OnModelCreating(modelBuilder);
        



    }



}

}