module Shared.Api

let wsEndpoint = "/appSock"

type Counter = { value : int }

type Msg =
    | Increment
    | Decrement

type UpstreamMsg =
    | AppMsgs of Msg list
    | GetState

type DownstreamMsg =
    | StateChange of Counter
    | Welcome

