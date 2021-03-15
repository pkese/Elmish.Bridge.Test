module Shared.Api


/// Defines how routes are generated on server and mapped from client
let routerPaths typeName method = sprintf "/api/%s" method

let wsEndpoint = "/appSock"

let condition = true

type State = { Counter: int }

type Msg =
    | Increment
    | Decrement

type UpstreamMsg =
    | AppMsg of Msg
    | GetState

type DownstreamMsg =
    | StateChange of State
    | Welcome

type Counter = { value : int }

/// A type that specifies the communication protocol between client and server
/// to learn more, read the docs at https://zaid-ajaj.github.io/Fable.Remoting/src/basics.html
type IServerApi = {
    Counter : unit -> Async<Counter>
    FullState : unit -> Async<Shared.Types.Model>
    ShortState : unit -> Async<Shared.Types.HwState>
    PostMsg : Shared.Types.AppMsg list -> Async<Shared.Types.Model>
}