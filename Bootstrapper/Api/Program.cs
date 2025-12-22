using System.Reflection;

WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);

//webAppBuilder.Logging.ClearProviders();
webAppBuilder.Host
  .UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
);

Assembly catalogAssembly = typeof(CatalogModule).Assembly;
Assembly basketAssembly = typeof(BasketModule).Assembly;
// add services to the container.
webAppBuilder.Services
  .AddCarterWithAssemblies(
    catalogAssembly,
    basketAssembly
  );

webAppBuilder.Services.AddMediatRWithAssemblies(
  catalogAssembly,
  basketAssembly
);

webAppBuilder.Services.AddStackExchangeRedisCache(options =>
{
  options.Configuration = webAppBuilder.Configuration.GetConnectionString("Redis");
});


webAppBuilder.Services
  .AddCatalogModule(webAppBuilder.Configuration)
  .AddOrderingModule(webAppBuilder.Configuration)
  .AddBasketModule(webAppBuilder.Configuration);

webAppBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();

WebApplication webApp = webAppBuilder.Build();

webApp.MapCarter();

webApp.UseSerilogRequestLogging();
webApp.UseExceptionHandler(options => { });

webApp
  .UseCatalogModule()
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
