module Main

open System
open Fable.Core.JsInterop

importAll "./styles/main.scss"

open Elmish
open Elmish.Bridge
open Elmish.React
open Elmish.Debug
open Elmish.HMR


let render model dispatch =
    App.render model (App.AppMsg >> dispatch)

// App
Program.mkProgram App.init App.update render
|> Program.withBridgeConfig (
    Bridge.endpoint Shared.Api.wsEndpoint
    |> Bridge.withWhenDown App.Disconnected
    |> Bridge.withMapping (fun bridgeMsg ->
        bridgeMsg |> App.NetworkMsg
    )
)
//|> Program.withSubscription App.channel.Subscription<App.State, App.Msg>
//-:cnd:noEmit
#if DEBUG
|> Program.withDebugger
#endif
//+:cnd:noEmit
|> Program.withReactSynchronous "feliz-app"
|> Program.run