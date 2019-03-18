namespace RangerFS.Tests

open Expecto
open FsCheck
open Ranger
open System

/// Example of a unit of measure type for testing
[<Measure>] type kilometre

/// Example of a custom type for use in ranges
type BoundedInt(n: int) =
    let value = 
        if n < -100 then -100
        else if n > 100 then 100
        else n

    member __.Value = value

    interface IComparable with
        member this.CompareTo obj = 
            match obj with
            | :? BoundedInt as that ->
                this.Value.CompareTo that.Value
            | _ ->
                failwith "Not an object"

    interface IComparable<BoundedInt> with
        member this.CompareTo that =
            this.Value.CompareTo that.Value

    interface IEquatable<BoundedInt> with
        
        member this.Equals that =
            this.Value = that.Value

    override this.Equals obj =
        match obj with
        | :? BoundedInt as that ->
            this.Value = that.Value
        | _ -> false
            
    override this.GetHashCode() =
        this.Value.GetHashCode()

    static member (+) (a: BoundedInt, b: BoundedInt) =
        BoundedInt(a.Value + b.Value)

    static member (-) (a: BoundedInt, b: BoundedInt) =
        BoundedInt(a.Value - b.Value)

    static member (*) (a: BoundedInt, b: BoundedInt) =
        BoundedInt(a.Value * b.Value)

    static member (/) (a: BoundedInt, b: BoundedInt) =
        BoundedInt(a.Value / b.Value)

    static member (~-) (a: BoundedInt) =
        BoundedInt(-a.Value)


[<AutoOpen>]
module Prelude = 
       
    let inline nonInf< ^T when ^T : (static member IsInfinity : ^T -> bool)> (num:^T) : bool =
        let inf = (^T : (static member IsInfinity : ^T -> bool) (num))
        not inf

    let inline nonNaN< ^T when ^T : (static member IsNaN: ^T -> bool)> (num:^T) : bool =
        let inf = (^T : (static member IsNaN : ^T -> bool) (num))
        not inf

    let nonNull x = 
        match box x with
        | null -> false
        | _ -> true

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
            |> Gen.filter nonNull
            |> Gen.listOfLength 3
            |> Gen.map Range.ofSeq
            |> Arb.fromGen

        static member inline NonEmptyRange< ^u when ^u:comparison>() =
            Arb.generate< ^u>
            |> Gen.filter nonNull
            |> Gen.listOfLength 3
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
