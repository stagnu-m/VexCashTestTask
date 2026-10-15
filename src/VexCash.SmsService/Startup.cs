using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using VexCash.SmsService.Data;
using VexCash.SmsService.Repositories;
using VexCash.SmsService.Services;
using VexCash.SmsService.Services.Options;

namespace VexCash.SmsService
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("VexCashSmsDb");
            });

            services.Configure<MessageBirdOptions>(
                Configuration.GetSection(MessageBirdOptions.SectionName));

            services.AddHttpClient<IMessageBirdClient, MessageBirdClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            // Repositories
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ITemplateRepository, TemplateRepository>();
            services.AddScoped<ISmsLogRepository, SmsLogRepository>();

            services.AddSingleton<ITemplateRenderer, TemplateRenderer>();
            services.AddScoped<ISmsDispatchService, SmsDispatchService>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "VexCash MessageBird SMS Service API",
                    Version = "v1",
                    Description = "REST API for managing customer credit reminders and dispatching SMS notifications via MessageBird."
                });

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDbContext dbContext)
        {
            DbInitializer.Seed(dbContext);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "VexCash SMS API v1");
                c.RoutePrefix = "swagger";
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", context =>
                {
                    context.Response.Redirect("/swagger");
                    return Task.CompletedTask;
                });
                endpoints.MapControllers();
            });
        }
    }
}
