module WebApp
    
open System.Reflection
open System
open Microsoft.AspNetCore.Http
open Giraffe

let versionChecker = 
    fun () -> 
        let att = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        if att = null then "" else att.InformationalVersion

type DoSomethingRequest = {
    Thing : string
}
        
let private doSomethingHandler request : HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            return! (setStatusCode 200 >=> text request.Thing) next ctx
        }

let webApp : (HttpFunc -> HttpContext -> HttpFuncResult) =
    choose [
        GET >=>
            choose [
                routeCix "/diagnostics/heartbeat(/?)" >=> setStatusCode 200 >=> warbler (fun _ -> (sprintf "Heartbeat: %d ticks" DateTime.Now.Ticks |> text))
                routeCix "/diagnostics/version(/?)" >=> setStatusCode 200 >=> warbler (fun _ -> versionChecker() |> text)
            ]
        POST >=> 
            choose [
                routeCix "/doSomething(/?)" >=> bindJson<DoSomethingRequest> doSomethingHandler
            ]
        setStatusCode 404 >=> text "Not Found" 
    ]

