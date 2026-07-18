using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

using RateAiArt.Configuration;
using RateAiArt.Data;
using RateAiArt.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

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
var kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);

builder.Services.AddScoped<AiEvaluationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
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
