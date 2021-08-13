using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoXaml.Framework.Extensions.DI;
using WpfNoXaml.Components;

namespace WpfNoXaml
{
    public class Startup: Application
    {
        public static readonly Type StartupWindow = typeof(MainWindow);

        private IServiceScope AppScope { get; }
        private IServiceProvider ServiceProvider => AppScope.ServiceProvider;
        private IConfiguration Configuration { get; }

        [STAThread]
        public static void Main(string[] args)
        {
            new Startup().Run();
        }

        public Startup()
        {
            Configuration = BuildConfiguration();

            var builder = new HostBuilder()
                .ConfigureLogging(ConfigureLogging)
                .ConfigureServices(ConfigureServices)
            ;

            var host = builder.Build();
            AppScope = host.Services.CreateScope();

            Startup += OnStartup;
            Exit += OnExit;
        }

        private IConfiguration BuildConfiguration()
        {
            var config = new ConfigurationBuilder();

            config
                .AddJsonFile("appsettings.json", false, false)
                // TODO anything else?

                // should be last
                //.AddFCLPConfiguration(ConfigureFCLP)
            ;

            return config.Build();
        }

        protected void OnStartup(object sender, StartupEventArgs e)
        {
            var window = (Window)ServiceProvider.GetRequiredService(StartupWindow);
            window.Show();
        }

        protected void OnExit(object sender, ExitEventArgs e)
        {
            AppScope.Dispose();
        }

        private void ConfigureLogging(ILoggingBuilder logging)
        {
            // nothing to do!
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services
                // singleton services
                .AddSingleton(new Random()) // example (we probably don't have much need for random data?)

                // scoped services
                // (any??)

                // transient services
                //services.AddDbContext<MyDbContext>(ConfigureDatabase, ServiceLifetime.Transient);

                // views
                .AddNoXamlComponents()
            ;
        }
        /*
        private void ConfigureDatabase(DbContextOptionsBuilder builder)
        {
            builder.UseSqlServer(Configuration.GetValue<string>("Db"));
        }

        private void ConfigureFCLP(FCLPConfigurationSource source)
        {
            source.OnHelp(s =>
            {
                ConsoleAllocator.ShowConsoleWindow();
                Console.Write(s);
                Console.Write("Press any key to exit...");
                Console.ReadKey();
                Application.Current.Shutdown();
            });
        }
        */
    }
}
