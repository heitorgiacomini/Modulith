using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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

webApp.UseExceptionHandler(exceptionHandlerApp =>
{
	exceptionHandlerApp.Run(async context =>
	{

		var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
		if (exception == null)
		{
			return;
		}

		ProblemDetails problemDetails = new()
		{
			Title = exception.Message,
			Status = StatusCodes.Status500InternalServerError,
			Detail = exception.StackTrace
		};

		var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
		logger.LogError(exception, exception.Message);

		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		context.Response.ContentType = "application/problem+json";
		await context.Response.WriteAsJsonAsync(problemDetails);
		//context.Response.StatusCode = StatusCodes.Status500InternalServerError;

		//// using static System.Net.Mime.MediaTypeNames;
		//context.Response.ContentType = Text.Plain;

		//await context.Response.WriteAsync("An exception was thrown.");

		//IExceptionHandlerPathFeature? exceptionHandlerPathFeature =
		//		context.Features.Get<IExceptionHandlerPathFeature>();

		//if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
		//{
		//	await context.Response.WriteAsync(" The file was not found.");
		//}

		//if (exceptionHandlerPathFeature?.Path == "/")
		//{
		//	await context.Response.WriteAsync(" Page: Home.");
		//}
	});
});
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
