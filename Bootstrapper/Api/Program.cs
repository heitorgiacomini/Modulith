var builder = WebApplication.CreateBuilder(args);

// add services to the container.
var app = builder.Build();

//Configure the HTTP request pipeline.

app.Run();
