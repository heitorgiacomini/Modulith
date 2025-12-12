WebApplicationBuilder webAppbuilder = WebApplication.CreateBuilder(args);

// add services to the container.
webAppbuilder.Services.AddCarterWithAssemblies(
	typeof(CatalogModule).Assembly
	);

webAppbuilder.Services
  .AddCatalogModule(webAppbuilder.Configuration)
  .AddOrderingModule(webAppbuilder.Configuration)
  .AddBasketModule(webAppbuilder.Configuration);

WebApplication webApp = webAppbuilder.Build();

webApp.MapCarter();

webApp.UseCatalogModule()
  .UseOrderingModule()
  .UseBasketModule();


//Configure the HTTP request pipeline.

//app.UseStaticFiles();

//app.UseRouting();

//app.UseAuthentication();

//app.UseAuthorization();


//app.UseEndpoints(endpoints =>
//{
//	endpoints.MapControllers();
//});	

webApp.Run();
