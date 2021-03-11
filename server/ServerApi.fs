module Server

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Shared.Api

open Hw
(*
open Elmish

let mutable prevModel : Model option = None
Program.mkProgram App.init App.update (fun model _ ->
    match prevModel with
    | Some m when m = model -> ()
    | _ ->
        prevModel <- Some model
        printf "%A\n" model
)
|> Program.run
*)

/// An implementation of the Shared IServerApi protocol.
/// Can require ASP.NET injected dependencies in the constructor and uses the Build() function to return value of `IServerApi`.
type ServerApi(logger: ILogger<ServerApi>, config: IConfiguration) =
    member this.Counter() =
        async {
            logger.LogInformation("Executing {Function}", "counter")
            do! Async.Sleep 1000
            return { value = 10 }
        }
    member this.FullState() = async {
        return Shared.Types.Model.initial
    }
    member this.ShortState() = async {
        return Shared.Types.Model.initial.hwState
    }
    member this.PostMsg (msgs: Shared.Types.AppMsg list) = async {
        return Shared.Types.Model.initial
    }

    member this.Build() : IServerApi =
        {
            Counter = this.Counter
            FullState = this.FullState
            ShortState = this.ShortState
            PostMsg = this.PostMsg
        }
