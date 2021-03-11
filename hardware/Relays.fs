module Relays

(*
remote debugging
https://www.jenx.si/2020/06/19/dot-net-core-remote-debugging-raspberry-pi/

When a Pi first boots:
- all Gpios are set to input
- GPIOs 0 to 8 (so including 5 & 6) have pull-ups enabled
- the rest have pull-downs enabled.
Later in the boot sequence many of them are set to ALT functions, though 5 & 6 aren't in that group.
*)

open System.Threading.Tasks

open System.Device.Gpio
open FSharp.Control.Tasks

open Shared.Types

type Channel = int

type Relays (gpio: GpioController) =
    let pinNumbers = [| 24; 25; 8; 7; 12; 16; 20; 21 |]
    let state = [| for p in pinNumbers -> Off |]
    do
        // open all pins and configure them as inputs
        match gpio.NumberingScheme with
        | PinNumberingScheme.Logical ->
            for pin in pinNumbers do
                gpio.OpenPin(pin, mode=PinMode.InputPullUp)
        | _
        | PinNumberingScheme.Board -> failwithf "Logical GPIO pin numbers expected"

    member _.NumChannels = pinNumbers.Length

    member _.Set (channel:Channel) value =
        let pin = pinNumbers.[channel]
        lock gpio (fun () ->
            match value with
            | On ->
                gpio.SetPinMode(pin, PinMode.Output)
                gpio.Write(pin, PinValue.Low)
            | Off ->
                if gpio.GetPinMode pin = PinMode.Output then
                    gpio.Write(pin, PinValue.High)
                    gpio.SetPinMode(pin, PinMode.InputPullUp)
            state.[channel] <- value
        )

    member _.Light (channel:Channel) value =
        lock gpio (fun () ->
            if state.[channel] = Off then
                let pin = pinNumbers.[channel]
                match value with
                | On -> gpio.SetPinMode(pin, PinMode.InputPullDown)
                | Off -> gpio.SetPinMode(pin, PinMode.InputPullUp)
        )


    member _.State = state
    member _.Get channel = state.[channel]

    member _.Lightup () =
        unitTask {
            for pin in pinNumbers do
                lock gpio (fun () -> 
                    if gpio.GetPinMode pin = PinMode.InputPullUp then
                        gpio.SetPinMode(pin, PinMode.InputPullDown)
                )
                do! Task.Delay(200)
                lock gpio (fun () -> 
                    if gpio.GetPinMode pin = PinMode.InputPullDown then
                        gpio.SetPinMode(pin, PinMode.InputPullUp)
                )
        }


let testRelays () =
    use gpio = new GpioController()
    let relays = Relays(gpio)

    let task = relays.Lightup()
    task.Wait()
    relays.Set 0 On
    Task.Delay(2000).Wait()
    printfn "relays done"


