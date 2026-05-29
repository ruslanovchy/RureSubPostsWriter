using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RureSubPostsWriter.Services;
using RureSubPostWriter.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddHostedService<OutboxCleaner>();

#region Db

var connectionString = builder.Configuration.GetConnectionString("Db");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Connection string is null or empty!");
}

builder.Services.AddDbContext<PostsWriterDbContext>(options =>
{
    options.UseNpgsql(connectionString)
        .UseLoggerFactory(LoggerFactory.Create(b => b.AddFilter((_,_) => false)));
});

#endregion

#region Jwt

var jwtKey = builder.Configuration["JWT:Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("Jwt key is null or empty!");
}

var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,

        IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes)
    };
});

#endregion

#region Kafka

var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"];
var groupId = builder.Configuration["Kafka:GroupId"];

var producerConfig = new ProducerConfig
{
    BootstrapServers = bootstrapServers,
};
var consumerConfig = new ConsumerConfig
{
    BootstrapServers = bootstrapServers,
    GroupId = groupId,
    EnableAutoCommit = false,
    EnableAutoOffsetStore = false,
    AutoOffsetReset = AutoOffsetReset.Earliest
};

builder.Services.AddSingleton(producerConfig);
builder.Services.AddSingleton(consumerConfig);

#endregion

#region Http

var ProfileApi = builder.Configuration["Http:ProfileApi"];

if (string.IsNullOrEmpty(ProfileApi))
{
    throw new Exception("Bad configuration! Http:Api is null or empty!");
}

builder.Services.AddHttpClient<IProfileApiClient, ProfileApiClient>(client => {
    client.BaseAddress = new Uri(ProfileApi);
});

#endregion

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
