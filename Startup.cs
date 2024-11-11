using Azure.Identity;
using Azure.Storage.Blobs;
using BackEnd.Entities;
using Microsoft.Azure.Cosmos;

namespace BackEnd
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            try
            {

                var keyVaultEndpoint = new Uri("https://tenxk.vault.azure.net/");
                var updatedConfiguration = new ConfigurationBuilder()
                    .AddConfiguration(_configuration)
                    .AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential())
                    .Build();


                var cosmosDbConnectionString = updatedConfiguration["CosmosDbConnectionString"];
                var blobConnectionString = updatedConfiguration["BlobConnectionString"];
                var apiKey = updatedConfiguration["ApiKey1"]; 

                if (string.IsNullOrEmpty(cosmosDbConnectionString) ||
                    string.IsNullOrEmpty(blobConnectionString) ||
                    string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("Connection strings or API key are missing.");
                }


                CosmosClientOptions clientOptions = new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    MaxRequestsPerTcpConnection = 10,
                    MaxTcpConnectionsPerEndpoint = 10
                };

                CosmosClient cosmosClient = new CosmosClient(cosmosDbConnectionString, clientOptions);
                services.AddSingleton(cosmosClient);
                services.AddScoped<CosmosDbContext>();


                services.AddSingleton(x => new BlobServiceClient(blobConnectionString));


                services.AddSingleton<IConfiguration>(updatedConfiguration);


                services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", builder =>
                    {
                        builder.AllowAnyOrigin()       
                               .AllowAnyHeader()       
                               .AllowAnyMethod();      
                    });
                });

                services.AddControllers();
                services.AddSwaggerGen();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfigureServices: {ex.Message}");
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseCors("AllowAll");

            app.UseMiddleware<ApiKeyMiddleware>();

            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
