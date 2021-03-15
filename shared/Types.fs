module Shared.Types


type TempSensor =
    | CevPecNot
    | CevPecVen
    | TempPecDrva
    | TempPecOlje
    | CevHisaNot
    | CevHisaVen
    | TempHisa
    | CevBojlerNot
    | CevBojlerVen
    | TempBoljer1
    | CevZalogZgoraj
    | CevZalogSredina
    | CevZalogSpodaj
    | TempZalogovnik1
    | Unassigned of string
  with
    static member name = function
        | CevPecNot -> "Cev: voda v peč"
        | CevPecVen -> "Cev: voda iz peči"
        | TempPecDrva -> "Temperatura peči na drva"
        | TempPecOlje -> "Temperatura peči na olje"
        | CevHisaNot -> "Cev: voda v hišo"
        | CevHisaVen -> "Cev: voda iz hiše"
        | TempHisa -> "Temperatura v hisi"
        | CevBojlerNot -> "Cev: voda v bojler"
        | CevBojlerVen -> "Cev: voda iz bojleja"
        | TempBoljer1 -> "Temperatura bojlerja"
        | CevZalogZgoraj -> "Cev: zalogovnik zgoraj"
        | CevZalogSredina -> "Cev: zalogovnik sredina"
        | CevZalogSpodaj -> "Cev: zalogovnik spodaj"
        | TempZalogovnik1 -> "Temperatura zalogovnika"
        | Unassigned addr -> $"Nedefiniran {addr}"
     
    
type RelayChannel = 
    | PumpaHisa
    | PumpaBojler
    | PumpaZalog
    | ZalogSmerHladna
    | PecOlje
    | VentilPecOlje
    | VentilPecDrva
  with
    static member name = function
        | PumpaHisa -> "Črpalka hiša"
        | PumpaBojler -> "Črpalka bojler"
        | PumpaZalog -> "Črpalka zalogovnik"
        | ZalogSmerHladna -> "Zalogovnik smer dol"
        | PecOlje -> "Vklop peči na olje"
        | VentilPecOlje -> "Ventil peč na olje"
        | VentilPecDrva -> "Ventil peč na drva"
    static member all = [
        PumpaHisa
        PumpaBojler
        PumpaZalog
        ZalogSmerHladna
        PecOlje
        VentilPecOlje
    ]

[<Struct>]
type RelayState =
    | On
    | Off

type TempSensorAddressMapping = Map<string,TempSensor>

type HwState = {
    temps: Map<TempSensor,float>
    relays: Map<RelayChannel, RelayState>
} with
    static member initial = {
        temps = Map.empty
        relays = [ for c in RelayChannel.all -> c, Off ] |> Map.ofSeq
    }


//todo: split into sysconfig + targets + state
type Model = {
    hwState: HwState
    targetTemp: float
    grejHiso: RelayState
    polniBojler: RelayState
    polniZalog: RelayState
    dovoliOlje: RelayState
    relayAssignment: Map<RelayChannel, int>
    tempAddrAssignment: Map<string, TempSensor>
  } with
    static member initial = {
        hwState = HwState.initial
        targetTemp = 21.5
        grejHiso = Off
        polniBojler = On
        polniZalog = Off
        dovoliOlje = Off
        relayAssignment = Map.empty
        tempAddrAssignment = Map.empty
    }

type HwMsg =
    | SwitchRelays of (RelayChannel*RelayState) list
    | SetRelayMapping of Map<RelayChannel, int>
    | SetTempAddrMapping of Map<string,TempSensor>
    | ApplyTempSensors of (string*float) list
    | KeepAlive of int*bool

type AppMsg =
    | PolniBojler of RelayState
    | PolniZalog of RelayState
    | DovoliOlje of RelayState
    | SetTargetTemp of float
    //| TempSensors of Map<TempSensor,float option>
    | SetTempAddrMapping of Map<string,TempSensor>
    | HwMsg of HwMsg

type UpstreamMsg =
    | FullModel of Model

type DownstreamMsg =
    | AppMsg of AppMsg    