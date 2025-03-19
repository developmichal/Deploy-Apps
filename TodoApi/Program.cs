using TodoApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// טעינת מחרוזת החיבור
var connectionString = builder.Configuration.GetConnectionString("ToDoDB") ?? Environment.GetEnvironmentVariable("ToDoDB");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("❌ מחרוזת החיבור לא נמצאה! בדוק משתני סביבה או appsettings.json");
}
else
{
    Console.WriteLine($"✅ מחרוזת חיבור נטענה: {connectionString}");
}

// הוספת ה-DbContext עם חיבור ל-MySQL
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString,
    new MySqlServerVersion(new Version(8, 0, 22)),
    mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

builder.Services.AddCors(option => option.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()));

builder.Logging.AddConsole();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

var app = builder.Build();

// בדיקת חיבור למסד הנתונים
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
    try
    {
        dbContext.Database.OpenConnection();
        Console.WriteLine("✅ הצלחנו להתחבר למסד הנתונים!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ חיבור למסד הנתונים נכשל: {ex.Message}");
    }
}

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "ToDoApi is running!");
app.MapGet("/selectAll", async (ToDoDbContext db) =>
{
    return await db.Items.ToListAsync();
});
app.MapPost("/add", async (ToDoDbContext db, string Name) =>
{
    var item = new Item { Name = Name, IsComplete = false };
    await db.Items.AddAsync(item);
    await db.SaveChangesAsync();
    return "Item added";
});

app.MapPatch("/update/{id}", async (ToDoDbContext db, int id, [FromBody] bool IsComplete) =>
{
    var existingItem = await db.Items.FindAsync(id);
    if (existingItem == null)
    {
        return Results.NotFound();
    }
    existingItem.IsComplete = IsComplete;
    await db.SaveChangesAsync();
    return Results.Ok("Item updated");
});

app.MapDelete("/delete/{id}", async (ToDoDbContext db, int id) =>
{
    var itemToDelete = await db.Items.FindAsync(id);
    if (itemToDelete == null)
    {
        return Results.NotFound();
    }
    db.Items.Remove(itemToDelete);
    await db.SaveChangesAsync();
    return Results.Ok("Item deleted");
});

app.Run();
