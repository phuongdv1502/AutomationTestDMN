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
builder.Services.AddTransient<GetConfigsUseCase>();
builder.Services.AddTransient<GetConfigUseCase>();
builder.Services.AddTransient<EvaluateDmnUseCase>();
builder.Services.AddTransient<TestDmnUseCase>();
builder.Services.AddTransient<GenerateTestFromDmnUseCase>();

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

app.Run(); 