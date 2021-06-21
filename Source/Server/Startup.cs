// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.Runtime.Management.GraphQL;
using GraphQL.Server.Ui.Playground;
using HotChocolate.AspNetCore;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace Dolittle.Runtime.Server
{

    /// <summary>
    /// The startup for Asp.Net Core.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configure all services.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to configure.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.AddCors(_ => _.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Grpc-Status", "Grpc-Message");
            }));

            services.AddGraphQLServer()
                    .AddInMemorySubscriptions()
                    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = RuntimeEnvironment.IsDevelopment)
                    .AddManagementAPI()
                    .AddType(new UuidType('D'));

            services.AddControllers();
            services.AddMvc();
            services.AddSwaggerGen();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/>.</param>
        /// <param name="env"><see cref="IWebHostEnvironment"/>.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Runtime API v1"));

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseGrpcWeb();
            app.UseCors();
            app.UseRouting();

            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                const string graphQLRoute = "/graphql";
                const string graphQLPlaygroundRoute = "/graphql/ui";

                endpoints.MapControllers();
                endpoints.MapGraphQLPlayground(
                    new PlaygroundOptions
                    {
                        GraphQLEndPoint = graphQLRoute,
                        SubscriptionsEndPoint = graphQLRoute
                    }, graphQLPlaygroundRoute);
                endpoints.MapGraphQL(graphQLRoute).WithOptions(new GraphQLServerOptions
                {
                    Tool = { Enable = false }
                });

                endpoints.MapMetrics(registry:app.ApplicationServices.GetService<CollectorRegistry>());
            });

            ObjectFieldExtensions.ServiceProvider = app.ApplicationServices;
            app.RunAsSinglePageApplication();
        }
    }
}
