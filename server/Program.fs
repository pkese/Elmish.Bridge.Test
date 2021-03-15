module Program

open Saturn
open Giraffe
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http

open Elmish
open Elmish.Bridge

let hub =
    ServerHub<unit, Shared.Api.UpstreamMsg, Shared.Api.DownstreamMsg>()

module GlobalCounter =
    open Shared.Api
    
    let init () = { value = 10 }, Cmd.none

    let update msg model =
        printfn "Got counter msg: %A" msg
        let model' : Counter =
            match msg with
            | AppMsg Increment -> { model with value = model.value + 1 }
            | AppMsg Decrement -> { model with value = model.value - 1 }
            | GetState -> hub.BroadcastClient (StateChange model); model
        if model' <> model then
            hub.BroadcastClient (StateChange model')
        model', Cmd.none

    let view model dispatch = ()

    let mutable private dispatchFn = fun msg -> ()
    let private registerDispatch dispatch =
        // grab `dispatch` and store it for posting messages from outside
        dispatchFn <- dispatch
        dispatch

    let dispatch msg = dispatchFn msg

    Program.mkProgram init update view
    |> Program.withSyncDispatch registerDispatch
    |> Program.run


module WebsocketClientApp =
    let init dispatch _  =
        printfn "socket client init()"
        let model = ()
        dispatch Shared.Api.DownstreamMsg.Welcome
        model, Cmd.none

    let update (dispatch: Dispatch<_>) msg model =
        //printfn "got websocket client msg: '%A'" msg
        GlobalCounter.dispatch msg
        model, Cmd.none

    let server =
        printfn "server initialized."
        Bridge.mkServer Shared.Api.wsEndpoint init update
        //|> Bridge.register Shared.Api.WebsocketServerMsg
        |> Bridge.withServerHub hub
        |> Bridge.run Giraffe.server


let webApp = choose [ 
    WebsocketClientApp.server
    GET >=> text "Welcome to full stack F#"
]

let serviceConfig (services: IServiceCollection) =
    services
      .AddLogging()


let application = application {
    use_router webApp
    use_static "wwwroot"
    app_config Giraffe.useWebSockets
    use_gzip
    //use_iis
    //add_channel "/channel" Channel.channel
    //url "http://0.0.0.0:5000"
    service_config serviceConfig
    webhost_config Env.configureHost
}

run application
