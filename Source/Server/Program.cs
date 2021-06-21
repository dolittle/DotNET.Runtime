// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Dolittle.Runtime.Hosting.Microsoft;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Dolittle.Runtime.Server
{
    static class Program
    {
        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">Arguments for the process.</param>
        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptions;

            await CreateHostBuilder(args)
                .Build()
                .RunAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Create a host builder.
        /// </summary>
        /// <param name="args">Arguments for the process.</param>
        /// <returns>Host builder to build and run.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var appConfig = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json")
                                    .Build();

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config => config.AddConfiguration(appConfig))
                .UseDolittle()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>();
                });
        }

        static void UnhandledExceptions(object sender, UnhandledExceptionEventArgs args)
        {
            if (args.ExceptionObject is Exception exception)
            {
                Console.WriteLine("************ BEGIN UNHANDLED EXCEPTION ************");
                PrintExceptionInfo(exception);

                while (exception.InnerException != null)
                {
                    Console.WriteLine("\n------------ BEGIN INNER EXCEPTION ------------");
                    PrintExceptionInfo(exception.InnerException);
                    exception = exception.InnerException;
                    Console.WriteLine("------------ END INNER EXCEPTION ------------\n");
                }

                Console.WriteLine("************ END UNHANDLED EXCEPTION ************ ");
            }
        }

        static void PrintExceptionInfo(Exception exception)
        {
            Console.WriteLine($"Exception type: {exception.GetType().FullName}");
            Console.WriteLine($"Exception message: {exception.Message}");
            Console.WriteLine($"Stack Trace: {exception.StackTrace}");
        }
    }
}
