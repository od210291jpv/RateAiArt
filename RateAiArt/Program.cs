using Microsoft.EntityFrameworkCore;

using Microsoft.OpenApi;
using Microsoft.SemanticKernel;

using RateAiArt.Configuration;
using RateAiArt.Data;
using RateAiArt.Services;

using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RateAiArt API",
        Version = "v1",
        Description = "API for rating AI-generated artwork"
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var ollamaSettings = builder.Configuration.GetSection("OllamaSettings").Get<OllamaSettings>() ?? new();
HttpClient ollamaHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
builder.Services.AddSingleton(ollamaHttpClient);

var kernelBuilder = builder.Services.AddKernel();
kernelBuilder.AddOpenAIChatCompletion(
    modelId: ollamaSettings.DefaultModelName,
    apiKey: "ignore-me",
    httpClient: ollamaHttpClient,
    endpoint: new Uri($"{ollamaSettings.Host}/v1"));

builder.Services.AddScoped<IAiEvaluationService, AiEvaluationService>();
builder.Services.AddScoped<ILeaderBoardService, LeaderBoardService>();
builder.Services.AddScoped<IImagesService, ImagesService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AiEndpointPolicy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RateAiArt API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// ¿ “»¬¿÷≤ﬂ RATE LIMITING
app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

app.Run();