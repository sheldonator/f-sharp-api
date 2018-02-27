module Config

type SerilogConfig() =    
    member val Hostname : string = "" with get, set
    member val Port : int = 0 with get, set
    member val LogFile : string = "" with get, set
    member val Facility : string = "" with get, set
    member val ApplicationName : string = "" with get, set
    member val ModuleName : string = "" with get, set
    member val AppApplicationName : string = "" with get, set
    