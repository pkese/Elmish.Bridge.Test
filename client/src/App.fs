module App

open Feliz
open Elmish
open Elmish.Bridge
open Shared.Api

type State = { Counter: Result<Counter, string> }

type ClientMsg =
    | AppMsg of Msg
    | NetworkMsg of DownstreamMsg
    | Disconnected

let init() = { Counter = Error "Waiting for connection..." }, Cmd.none

let update (msg: ClientMsg) (state: State) : State * Cmd<ClientMsg> =
    printfn "elmish update: %A" msg
    match msg with
    | AppMsg Increment ->
        state, Cmd.bridgeSend <| UpstreamMsg.AppMsg Increment
    | AppMsg Decrement ->
        state, Cmd.bridgeSend <| UpstreamMsg.AppMsg Decrement
    | NetworkMsg Welcome ->
        printfn "got Welcome"
        { Counter = Error "Waiting for counter state..." }, Cmd.bridgeSend UpstreamMsg.GetState
    | NetworkMsg (StateChange newState) ->
        printfn "got counter %A" newState
        { state with Counter = Ok newState }, Cmd.none
    | Disconnected ->
        printfn "Disconnected"
        { state with Counter = Error "Disconnected" }, Cmd.none

let renderCounter (counter: Result<Counter, string>)=
    match counter with
    | Ok counter -> Html.h1 counter.value
    | Error errorMsg ->
        Html.h1 [
            prop.style [ style.color.crimson ]
            prop.text errorMsg
        ]

let fableLogo() = StaticFile.import "./imgs/fable_logo.png"

let render (state: State) (dispatch: Msg -> unit) =

    Html.div [
        prop.style [
            style.textAlign.center
            style.padding 40
        ]

        prop.children [

            Html.img [
                prop.src(fableLogo())
                prop.width 250
            ]

            Html.h1 "Full-Stack Counter"

            Html.button [
                prop.style [ style.margin 5; style.padding 15 ]
                prop.onClick (fun _ -> dispatch Increment)
                prop.text "Increment"
            ]

            Html.button [
                prop.style [ style.margin 5; style.padding 15 ]
                prop.onClick (fun _ -> dispatch Decrement)
                prop.text "Decrement"
            ]

            renderCounter state.Counter
        ]
    ]