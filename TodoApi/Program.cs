using TodoApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>(options =>
options.UseMySql(builder.Configuration.GetConnectionString("ToDoDb"), 
    new MySqlServerVersion(new Version(8, 0, 41)))); 

builder.Services.AddCors(option => option.AddPolicy("AllowAll",//נתינת שם להרשאה
    p => p.AllowAnyOrigin()//מאפשר כל מקור
    .AllowAnyMethod()//כל מתודה - פונקציה
    .AllowAnyHeader()));//וכל כותרת פונקציה

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});
var app = builder.Build();
app.UseCors("AllowAll");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");

app.MapGet("/selectAll", async (ToDoDbContext db) =>{
    return await db.Items.ToListAsync();

} );
app.MapPost("/add", async (ToDoDbContext db,string Name) =>{
    var item = new Item { Name = Name, IsComplete = false }; 
     await db.Items.AddAsync(item);
     await db.SaveChangesAsync();
     return "Item added";
} );

app.MapPatch("/update/{id}", async (ToDoDbContext db, int id, [FromBody] bool IsComplete) => {
    var existingItem = await db.Items.FindAsync(id);
    
    if (existingItem == null)
    {
        return Results.NotFound(); 
    }
    
    existingItem.IsComplete = IsComplete;
    
    await db.SaveChangesAsync();
    return Results.Ok("Item updated"); 
});


app.MapDelete("/delete/{id}", async (ToDoDbContext db, int id) => {
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
