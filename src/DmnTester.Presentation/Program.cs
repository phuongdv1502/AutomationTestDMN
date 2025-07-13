using DmnTester.Domain.Interfaces;
using DmnTester.Infrastructure.Services;
using DmnTester.Application.UseCases;
using DmnTester.Presentation.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure timeout settings
var timeoutSettings = new TimeoutSettings();
builder.Configuration.GetSection("TimeoutSettings").Bind(timeoutSettings);
builder.Services.AddSingleton(timeoutSettings);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Dependency Injection
builder.Services.AddSingleton<IDmnEngine>(provider => 
    new DmnEngine(timeoutSettings.DmnEvaluationTimeout, timeoutSettings.FileReadTimeout));
builder.Services.AddSingleton<IDmnTestGenerator, DmnTestGenerator>();
builder.Services.AddSingleton<IDmnFileAnalyzer, DmnFileAnalyzer>();
builder.Services.AddTransient<GetConfigsUseCase>();
builder.Services.AddTransient<GetConfigUseCase>();
builder.Services.AddTransient<EvaluateDmnUseCase>();
builder.Services.AddTransient<TestDmnUseCase>();
builder.Services.AddTransient<GenerateTestFromDmnUseCase>();
builder.Services.AddTransient<AnalyzeDmnFileUseCase>();
builder.Services.AddTransient<ExtractDecisionUseCase>();
builder.Services.AddTransient<GetDecisionSummariesUseCase>();
builder.Services.AddTransient<GetDecisionChunksUseCase>();
builder.Services.AddTransient<GetDecisionChunkUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Thêm middleware bắt exception toàn cục
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = new { error = ex.Message, detail = ex.InnerException?.Message };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(error));
        // Có thể log ra file hoặc hệ thống log ở đây nếu muốn
    }
});

app.Run(); 