open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Giraffe
open Giraffe.Middleware
open Serilog
open Serilog.Events
open Serilog.Exceptions
open Middleware
open ConfigManager
open Microsoft.Extensions.Logging
open WebApp
open Config

type Startup private () = 
    new (configuration :IConfiguration) =
        Startup() then
        Instance.StoreConfiguration configuration

    static member ErrorHandler (ex : Exception) (logger : Microsoft.Extensions.Logging.ILogger) =
        Log.Logger.Error("An unhandled exception has occurred while executing the request. {ex}", ex)
        clearResponse >=> setStatusCode 500 >=> text ex.Message

    member this.Configure (builder: IApplicationBuilder) (env: IHostingEnvironment) (loggerFactory: Microsoft.Extensions.Logging.ILoggerFactory) : unit =
        builder.UseAuthentication()
            .UseGiraffeErrorHandler(Startup.ErrorHandler)
            .UseGiraffe(webApp)  

    member this.ConfigureServices (services : IServiceCollection) : unit =
        let configManager = Instance.Get
        let serilogConfig = new SerilogConfig()
        configManager.GetSection("SerilogConfig").Bind(serilogConfig)
        services
            .Configure<SerilogConfig>(fun c -> configManager.GetSection("SerilogConfig").Bind(c))
            .AddSingleton(serilogConfig) 
            .AddGiraffe()
            .AddMvc() |> ignore

let private configureAppConfiguration (hostingContext: WebHostBuilderContext) (config : IConfigurationBuilder) =
    let env = hostingContext.HostingEnvironment
    config
        .AddJsonFile("appsettings.json")            
        .AddJsonFile((sprintf "appsettings.%s.json" env.EnvironmentName), optional=true) 
        .AddEnvironmentVariables() |> ignore

let private configureLogging (context: WebHostBuilderContext) (builder : ILoggingBuilder)  =
    let filter (l : LogLevel) = l.Equals LogLevel.Debug
    let serilogConfig = new SerilogConfig()
    context.Configuration.GetSection("SerilogConfig").Bind(serilogConfig)
    let mutable logger = new Serilog.LoggerConfiguration()
    logger.WriteTo.File(serilogConfig.LogFile.Replace("[TODAY]", DateTime.Now.Date.ToString("yyyy-MM-dd")),
        outputTemplate = "{Timestamp:HH:mm:ss} [{Level}] CorrelationId: {CorrelationId} Message: {Message}{NewLine}{Exception}")
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext() 
        .Enrich.WithExceptionDetails() |> ignore
    Log.Logger <- logger.CreateLogger()
    builder.AddSerilog() |> ignore

[<EntryPoint>]
let main argv =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    let app = 
        WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(contentRoot)
            .UseIISIntegration()
            .UseWebRoot(webRoot)
            .ConfigureAppConfiguration(configureAppConfiguration)
            .ConfigureLogging(configureLogging)
            .UseStartup<Startup>()
            .UseSerilog()
            .Build()
    Serilog.Log.Logger.Information((System.String.Format("f-sharp-api version {0} starting now", (versionChecker()))))
    app.Run()
    Serilog.Log.Logger.Information((System.String.Format("f-sharp-api version {0} shutting down", (versionChecker()))))
    0
