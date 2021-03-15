module Shared.Api

let wsEndpoint = "/appSock"

type Counter = { value : int }

type Msg =
    | Increment
    | Decrement

type UpstreamMsg =
    | AppMsg of Msg
    | GetState

type DownstreamMsg =
    | StateChange of Counter
    | Welcome

