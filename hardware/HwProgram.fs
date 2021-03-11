// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
module Hw

open System

open System.Device.Gpio
open Elmish

open Relays
open Shared.Types


module HwApp =

    let delayMsg (timeoutMs:int) cmd =
        Cmd.OfAsync.result (
            async {
                do! Async.Sleep timeoutMs
                return cmd
            })

    let update (relays:Relays) msg state =
        match msg with

        | HwMsg.SwitchRelays changes ->
            let newRelayStates =
                (state.hwState.relays, changes)
                ||> List.fold (fun relayStates (relay,value) ->
                    match state.relayAssignment |> Map.tryFind relay with
                    | Some channel ->
                        relays.Set channel value
                        relayStates |> Map.add relay value
                    | None ->
                        relayStates
                )
            if newRelayStates <> state.hwState.relays then
                { state with hwState = { state.hwState with relays = newRelayStates }}, Cmd.none
            else state, Cmd.none

        | HwMsg.SetRelayMapping mapping ->
            let newAssignment = 
                (mapping, state.relayAssignment)
                ||> Map.fold (fun assignment relay channel ->
                    assignment |> Map.add relay channel
                )
            { state with relayAssignment = newAssignment }, Cmd.none

        | HwMsg.SetTempAddrMapping mapping ->
            let newAssignment = 
                (mapping, state.tempAddrAssignment)
                ||> Map.fold (fun assignment addr sensor ->
                    assignment |> Map.add addr sensor
                )
            { state with tempAddrAssignment = newAssignment }, Cmd.none

        | HwMsg.ApplyTempSensors sensorReadings ->
            let initialTemps =
                //state.hwState.temps
                Map.empty
            let newTemps =
                (initialTemps, sensorReadings)
                ||> List.fold (fun temps (addr, temp) ->
                    match state.tempAddrAssignment |> Map.tryFind addr with
                    | Some sensor ->
                        temps |> Map.add sensor temp
                    | None ->
                        temps |> Map.add (Unassigned addr) temp
                )
            if state.hwState.temps <> newTemps then
                { state with hwState = { state.hwState with temps = newTemps }}, Cmd.none
            else
                state, Cmd.none

        | HwMsg.KeepAlive (channel,dir) ->
            relays.Light channel Off
            let dir', channel' =
                if dir then
                    if channel+1 = relays.NumChannels
                    then (not dir), channel-1
                    else dir, channel+1
                else
                    if channel = 0
                    then (not dir), 1
                    else dir, channel-1
            relays.Light channel' On
            
            state, delayMsg 250 (HwMsg (HwMsg.KeepAlive (channel', dir')))

            

let gpio = new GpioController()
let relays = Relays(gpio)

module App = 
    let control state =
        let getTemp sensor = state.hwState.temps |> Map.tryFind sensor
        let tCevPecNot = getTemp CevPecNot
        let tCevPecVen = getTemp CevPecVen
        let tTempPecDrva = getTemp TempPecDrva
        let tTempPecOlje = getTemp TempPecOlje
        let tCevHisaNot = getTemp CevHisaNot
        let tCevHisaVen = getTemp CevHisaVen
        let tTempHisa = getTemp TempHisa
        let tCevBojlerNot = getTemp CevBojlerNot
        let tCevBojlerVen = getTemp CevBojlerVen
        let tTempBoljer1 = getTemp TempBoljer1
        let tCevZalogZgoraj = getTemp CevZalogZgoraj
        let tCevZalogSredina = getTemp CevZalogSredina
        let tCevZalogSpodaj = getTemp CevZalogSpodaj
        let tTempZalogovnik1 = getTemp TempZalogovnik1

        let heatSource =
            match tTempPecDrva, tTempPecOlje with
            | Some x, Some y -> max x y
            | Some x, None -> x
            | None, Some x -> x
            | None, None -> 0.0

        let setRelay relay value =
            match state.hwState.relays |> Map.tryFind relay with
            | Some x when x = value -> []
            | Some x -> [(relay,value)]
            | None -> []

        let getRelay relay = state.hwState.relays |> Map.tryFind relay |> Option.defaultValue Off
        let relayHisa = getRelay PumpaHisa
        let relayBojler = getRelay PumpaBojler
        let relayZalog = getRelay PumpaZalog

        let relaySwitches = [

            let overheatingBy =
                let cev = tCevPecVen |> Option.defaultValue 0.0
                max (heatSource - 92.0) (cev - 85.0)

            if overheatingBy > 0.0 then
                if overheatingBy > 1.0 then yield (PumpaBojler, On)
                if overheatingBy > 2.0 then yield (PumpaHisa, On)
                if overheatingBy > 4.0 then
                    yield! setRelay PumpaZalog On
                    yield! setRelay ZalogSmerHladna On
            else
                // crpalka hisa
                if state.grejHiso = On then
                    let termostatDelta =
                        match tTempHisa, state.grejHiso with
                        | Some temp, _ -> temp - state.targetTemp
                        | None, Off -> -100.0
                        | None, On -> +100.0
                    if relayHisa = Off then
                        if termostatDelta > 0.1 then yield (PumpaHisa,On)
                    else
                        if termostatDelta < -0.1 then yield (PumpaHisa,Off)
                elif relayHisa = On then
                    yield (PumpaHisa,Off)

                // crpalka bojler
                if state.polniBojler = On then
                    let tBojler = tTempBoljer1 |> Option.defaultValue 60.0
                    let tVodaIzPeči = tCevPecVen |> Option.defaultValue heatSource
                    let tempDelta = tVodaIzPeči - tBojler
                    if relayBojler = Off then
                        if tempDelta > 5.0 then yield (PumpaBojler,On)
                    else
                        if tempDelta <= 2.0 then yield (PumpaBojler,Off)
                elif relayBojler = On then
                    yield (PumpaBojler,Off)

                // crpalka zalogovnik
                if state.polniZalog = On then
                    let tZalog = tTempBoljer1 |> Option.defaultValue 45.0
                    let tVodaIzPeči = tCevPecVen |> Option.defaultValue heatSource
                    let tempDelta = tVodaIzPeči - tZalog
                    if relayZalog = Off then
                        if tempDelta > 1.0 then yield (PumpaZalog,On)
                    else
                        if tempDelta < -1.0 then yield (PumpaZalog,Off)
                elif relayZalog = On then
                    yield (PumpaZalog,Off)
                    if getRelay ZalogSmerHladna = On then
                        yield (ZalogSmerHladna, Off) 

                // smer vode iz zalogovnika
                if relayZalog = On then
                    let tZalog = tTempBoljer1 |> Option.defaultValue 45.0
                    let hladnaSmer = getRelay ZalogSmerHladna = On
                    let vročaVoda = tZalog > 75.0
                    if vročaVoda && not hladnaSmer then
                        yield (ZalogSmerHladna, if vročaVoda then On else Off)
                elif relayHisa = On then
                    if getRelay ZalogSmerHladna = On then
                        yield (ZalogSmerHladna, Off) 

        ]
        Cmd.ofMsg (HwMsg (HwMsg.SwitchRelays relaySwitches))

    let update msg state =
        let newState, cmd =
            match msg with
            | SetTempAddrMapping mapping ->
                HwApp.update relays (HwMsg.SetTempAddrMapping mapping) state
            | HwMsg msg ->
                HwApp.update relays msg state
            | PolniBojler value -> { state with polniBojler = value }, []
            | PolniZalog value -> { state with polniZalog = value }, []
            | DovoliOlje value -> { state with dovoliOlje = value }, []
            | SetTargetTemp value -> { state with targetTemp = value }, []
            //| TempSensors of Map<TempSensor,float option>

        let cmds =
            if state <> newState then
                let controlCmd = newState |> control
                Cmd.batch [ cmd ; controlCmd ]
            else
                cmd

        newState, cmds

    let init () =
        Shared.Types.Model.initial, Cmd.ofMsg (HwMsg (KeepAlive(1,false)))


let testHw () =
    let mutable prevModel : Model option = None
    Program.mkProgram App.init App.update (fun model _ ->
        match prevModel with
        | Some m when m = model -> ()
        | _ ->
            prevModel <- Some model
            printf "%A\n" model
    )
    |> Program.run


[<EntryPoint>]
let main argv =
    testHw()
    printfn "Temps:"
    TempSensors.fetchAll ()
    |> Seq.iter (printfn "%A")
    //Relays.testRelays ()

    Async.Sleep 30000 |> Async.RunSynchronously

    printfn "done."
    0 // return an integer exit code