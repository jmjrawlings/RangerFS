namespace RangerFS.Tests

open Expecto
open FsCheck
open Ranger
open System

[<AutoOpen>]
module Prelude = 

    let not_null x =
        match box x with
        | null -> false
        | _ -> true
       
    let inline nonInf< ^T when ^T : (static member IsInfinity : ^T -> bool)> (num:^T) : bool =
        let inf = (^T : (static member IsInfinity : ^T -> bool) (num))
        not inf

    let inline nonNaN< ^T when ^T : (static member IsNaN: ^T -> bool)> (num:^T) : bool =
        let inf = (^T : (static member IsNaN : ^T -> bool) (num))
        not inf

    let year = TimeSpan.FromDays 365.

    type NonEmptyRange<'t when 't:comparison> =
        { Range : 't Range }

    /// A range and a point within it
    type RangeAndPoint<'t when 't:comparison> =
        { Range: 't Range; Point: 't}
    
    type Generators =

        static member Float() =
            Arb.Default.Float()
            |> Arb.filter (fun x -> nonInf x && nonNaN x)

        static member Float32() =
            Arb.Default.Float32()
            |> Arb.filter (fun x -> nonInf x && nonNaN x)

        static member DateTime() =
            Arb.Default.DateTime()
            |> Arb.filter (fun x -> x > DateTime.MinValue + year)
            |> Arb.filter (fun x -> x < DateTime.MaxValue - year)

        static member TimeSpan() =
            Arb.Default.TimeSpan()
            |> Arb.filter (fun x -> x < year)
            |> Arb.filter (fun x -> x > - year)

        static member inline GenericRange< ^u when ^u:comparison>() =
            Arb.generate< ^u>
            |> Gen.filter not_null
            |> Gen.listOf
            |> Gen.map Range.ofSeq
            |> Arb.fromGen

        static member inline NonEmptyRange< ^u when ^u:comparison>() =
            Arb.generate< ^u>
            |> Gen.filter not_null
            |> Gen.listOfLength 10
            |> Gen.map Range.ofSeq
            |> Gen.filter (Range.isEmpty >> not)
            |> Gen.map (fun r -> {Range=r})
            |> Arb.fromGen

    let testPropertyWithConfig config name test =
        testPropertyWithConfig
            { config with arbitrary = [ typeof<Generators> ] }
            name test

    let testProp name test =
        testPropertyWithConfig
            FsCheckConfig.defaultConfig name test
