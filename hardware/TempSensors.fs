module TempSensors

open System
open Fake.IO.Globbing
open Fake.IO.Globbing.Operators

(*
We have DS2482-100 1-wire bus driver chip

To wake it up, do a:

    modprobe ds2482
    echo ds2482 0x18 > /sys/bus/i2c/devices/i2c-0/new_device


Tell `wire` kernel module that we will have more slave devices
    modprobe wire max_slave_count=30

Tell `w1` bus master to scan for new devices (or to stop scanning)
echo 0 > /sys/bus/w1/devices/w1_bus_master1/w1_master_search
echo 0 > /sys/bus/w1/devices/w1_bus_master2/w1_master_search

*)


open System.Text.RegularExpressions

let regex = Regex(@".*\/28-(\w+)\/.*$", RegexOptions.Compiled)
let extractAddress s = 
    let m = regex.Match s
    match m.Groups.Count with
    | 2 -> Some m.Groups.[1].Value
    | _ -> None

//extractAddress "/sys/bus/w1/devices/28-013e2d07010c/temperature"

let fetchAll () =
    //let globPattern = "/sys/bus/w1/devices/28-*/w1_slave"
    [ for fpath in !! "/sys/bus/w1/devices/28-**/temperature" do
        //printfn "fpath: %s" fpath
        // enumerate all files matching pattern
        let fileData = IO.File.ReadAllText fpath
        match Double.TryParse fileData with
        | true, rawTemp ->
            match extractAddress fpath with
            | Some addr -> yield (addr, rawTemp / 1000.0)
            | None -> ()
        | false, _ -> ()
    ]
