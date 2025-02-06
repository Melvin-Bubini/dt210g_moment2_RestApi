using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseSqlite("Data Source=todo.db"));  // Skapa en SQLite-databasfil som heter 'todo.db'
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});

// Lägger till CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Tillåt anrop från frontend
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

// Aktiverar CORS 
app.UseCors("AllowFrontend");

app.MapGet("/todoitems", async (TodoDb db) =>
    await db.Todos.ToListAsync());

app.MapGet("/todoitems/notStarted", async (TodoDb db) =>
{
    // Hämta alla todos där status är "Ej påbörjad"
    var notStartedTodos = await db.Todos
                                   .Where(todo => todo.Status == "Ej påbörjad")
                                   .ToListAsync();

    return Results.Ok(notStartedTodos);
});

app.MapGet("/todoitems/inProgress", async (TodoDb db) =>
{
    // Hämta alla todos där status är "Pågående"
    var inProgressTodos = await db.Todos
                                   .Where(todo => todo.Status == "Pågående")
                                   .ToListAsync();

    return Results.Ok(inProgressTodos);
});

app.MapGet("/todoitems/completed", async (TodoDb db) =>
{
    // Hämta alla todos där status är "Avklarad"
    var completedTodos = await db.Todos
                                 .Where(todo => todo.Status == "Avklarad")
                                 .ToListAsync();

    return Results.Ok(completedTodos);
});


app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());

app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Title = inputTodo.Title;
    todo.Description = inputTodo.Description;
    todo.Status = inputTodo.Status;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();
