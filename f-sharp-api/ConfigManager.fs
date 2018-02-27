module ConfigManager
    
    open Microsoft.Extensions.Configuration

    type ConfigHolder internal () =
        let mutable configuration : IConfiguration = null

        member this.StoreConfiguration (newConfiguration : IConfiguration) =
            configuration <- newConfiguration
        member this.Get =
            configuration

    let Instance = ConfigHolder()

