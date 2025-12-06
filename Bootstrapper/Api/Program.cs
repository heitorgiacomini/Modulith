

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// add services to the container.

builder.Services
  .AddCatalogModule(builder.Configuration)
  .AddOrderingModule(builder.Configuration)
  .AddBasketModule(builder.Configuration);

WebApplication app = builder.Build();

app.UseCatalogModule()
  .UseOrderingModule()
  .UseBasketModule();

//app.UseCatalogModule();
//Configure the HTTP request pipeline.

app.UseStaticFiles();

//app.UseRouting();

//app.UseAuthentication();

//app.UseAuthorization();


//app.UseEndpoints(endpoints =>
//{
//	endpoints.MapControllers();
//});	

app.Run();
