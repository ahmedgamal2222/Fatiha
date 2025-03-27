using Fatiha__app.Data;
using Fatiha__app.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Options;
using SendGrid;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Identity.UI.Services;
var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 1. إعداد سلسلة الاتصال والخدمات الأساسية
// ==================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// إضافة HttpClient
builder.Services.AddHttpClient();

// تسجيل خدمات رفع الملفات (اختر نمط التسجيل المناسب)
builder.Services.AddScoped<DigitalOceanSpaceUploaderService>();
builder.Services.AddTransient<FileUploadService>();

// تسجيل DbContext باستخدام SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// إعداد Identity مع دعم الأدوار
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ==================================================================
// 2. إعداد Controllers مع JSON والتوطين
// ==================================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
    });


// تسجيل خدمات التوطين وتحديد مسار الموارد
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

// تهيئة خيارات طلب التوطين والثقافات المدعومة
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("ar-SA"),
        new CultureInfo("en-US"),
        new CultureInfo("zh-CN"),
        new CultureInfo("es-ES"),
        new CultureInfo("hi-IN"),
        new CultureInfo("fr-FR"),
        new CultureInfo("ru-RU"),
        new CultureInfo("bn-BD"),
        new CultureInfo("pt-BR"),
        new CultureInfo("ur-PK"),
        new CultureInfo("id-ID")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// ==================================================================
// 3. إعداد JWT Authentication
// ==================================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});


// ==================================================================
// 4. إعداد Swagger
// ==================================================================
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();

builder.Services.AddHttpsRedirection(options => {
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fatiha API",
        Version = "v1",
        Description = "API Documentation for Fatiha__app"
    });

    // إضافة دعم رفع الملفات
    options.OperationFilter<FormFileOperationFilter>();

    // دعم المصادقة عبر JWT في Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});



// ==================================================================
// 5. إعداد CORS (سياسة السماح بكل شيء)
// ==================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder.SetIsOriginAllowed(_ => true) // ✅ السماح بأي Origin
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .WithExposedHeaders("Authorization") // ✅ كشف التوكن في الردود
                     .AllowCredentials();


    });
});

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(@"C:\Users\store\Downloads\fatiha-cb547-firebase-adminsdk-fbsvc-723d92dd8e.json")
});


// ==================================================================
// 6. بناء التطبيق واستخدام Middleware
// ==================================================================
var app = builder.Build();

// تفعيل التوطين
app.UseRequestLocalization();

// تفعيل CORS قبل الـ Middleware الخاصة بالتوجيه والمصادقة
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fatiha API v1");
    });
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// تفعيل المصادقة والتفويض
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ==================================================================
// 7. تهيئة الأدوار وإنشاء المستخدم الإداري الافتراضي
// ==================================================================

// تهيئة الأدوار
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admins", "Instructors" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// إنشاء مستخدم إداري افتراضي
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    string email = "1@gmail.com";
    string password = "1*bQ2958";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new IdentityUser
        {
            Email = email,
            UserName = email
        };

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, "Admins");
    }
}

app.Run();
