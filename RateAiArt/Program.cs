using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;

using RateAiArt.Configuration;
using RateAiArt.Data;
using RateAiArt.Services;

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

// Build the kernel and register it in DI


builder.Services.AddScoped<IAiEvaluationService, AiEvaluationService>();

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
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

app.Run();
