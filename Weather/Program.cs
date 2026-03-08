using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Weather.Data;
using Weather.Endpoints;
using Weather.Services;
using Weather.Configurations;
using Weather.Interfaces;
using Weather.Middlewares;
using Weather.Services.WeatherService;

var builder = WebApplication.CreateBuilder(args);

// Добавляем OpenAPI (для документации)
builder.Services.AddOpenApi();

// Добавляем контекст базы данных SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IAuthService, AuthService>();
// Привязываем настройки JWT из конфигурации
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.AddSingleton(jwtSettings); // делаем доступным для внедрения

// Добавляем аутентификацию JWT
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero // убираем задержку по умолчанию (5 минут)
        };
    });
// Добавляем авторизацию (пока без политик)
builder.Services.AddAuthorization();

// Регистрация HttpClient для каждого провайдера с базовым адресом
builder.Services.AddHttpClient<OpenWeatherMapProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.openweathermap.org/");
});

builder.Services.AddHttpClient<WeatherApiProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.weatherapi.com/");
});
// Добавляем HttpClient для wttr.in
builder.Services.AddHttpClient<WttrInProvider>(client =>
{
    client.BaseAddress = new Uri("https://wttr.in/");
    // wttr.in может блокировать запросы без User-Agent
    client.DefaultRequestHeaders.Add("User-Agent", "WeatherApp/1.0");
});
// Регистрируем интерфейсы, используя уже зарегистрированные типы
builder.Services.AddScoped<IWeatherProvider, OpenWeatherMapProvider>(sp =>
    sp.GetRequiredService<OpenWeatherMapProvider>());

builder.Services.AddScoped<IWeatherProvider, WeatherApiProvider>(sp =>
    sp.GetRequiredService<WeatherApiProvider>());

// Регистрируем провайдер
builder.Services.AddScoped<IWeatherProvider, WttrInProvider>(sp =>
    sp.GetRequiredService<WttrInProvider>());


builder.Services.AddScoped<IWeatherAggregator, WeatherAggregator>();

builder.Services.AddMemoryCache();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Приложение запущено и готово к работе");
// Автоматическое применение миграций при запуске (удобно для разработки)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Настраиваем HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // эндпоинт для OpenAPI документации (JSON)
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Incoming request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
});
// Здесь позже будут наши эндпоинты (регистрация, логин, погода)
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapWeatherEndpoints();

app.Run();