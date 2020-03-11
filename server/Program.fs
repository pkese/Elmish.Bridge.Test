﻿module Program

open Saturn
open Giraffe
open Shared
open Server
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.Extensions.DependencyInjection

let webApi =
    Remoting.createApi()
    |> Remoting.fromContext (fun ctx -> ctx.GetService<ServerApi>().Build())
    |> Remoting.withRouteBuilder routerPaths
    |> Remoting.buildHttpHandler

let webApp = choose [ webApi; GET >=> text "Hello to full STACK F#" ]

let serviceConfig (services: IServiceCollection) =
    services
      .AddSingleton<ServerApi>()
      .AddLogging()

let application = application {
    use_router webApp
    use_static "wwwroot"
    use_gzip
    use_iis
    service_config serviceConfig
    host_config Env.configureHost
}

run application