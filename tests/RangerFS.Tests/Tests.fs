namespace RangerFS.Tests

open Expecto
open FsCheck
open System
open Ranger
open Ranger.Operators

open Prelude

#nowarn "0686"

module ConstructionTests =

    let make<'t when 't:comparison> () : Test = 

      let type' = typeof<'t>
            
      testList (sprintf "%s" type'.Name) [
        
          testProp "order doesn't matter for creation"
            <| fun (lo: 't, hi: 't) ->
                (Range.ofBounds lo hi) = (Range.ofBounds hi lo)

          testProp "same value is a point"
            <| fun (x: 't) ->
                let r1 = Range.ofBounds x x
                let r2 = Range.ofPoint x
                r1 = r2 && r1.IsPoint && r2.IsPoint

          testProp "ofSeq works"
            <| fun (xs: 't list) ->
                let r = Range.ofSeq xs
                match xs with
                | [] -> r.IsEmpty
                | _  -> not r.IsEmpty

      ]


    [<Tests>]
    let tests = 
        testList "creation" [
            make<DateTime>()
            make<Int16>()
            make<Int32>()
            make<Int64>()
            make<char>()
            make<float>()
            make<float32>()
            make<BoundedInt>()
        ]        


module LogicTests = 

    let make<'t when 't:comparison> () : Test =

        let type' = typeof<'t>
            
        testList (sprintf "%s" type'.Name) [

            testProp "union with self is self"
                <| fun (x: 't Range) ->
                Range.union x x = x

            testProp "union is commutative"
                <| fun (a: 't Range, b: 't Range) ->
                (Range.union a b) = (Range.union b a)

            testProp "disjoin opposite of intersect"
                <| fun (a: 't Range, b: 't Range) ->
                (Range.intersects a b) <> (Range.disjoint a b)

            testProp "union many works" 
                <| fun (xs: List<'t>) ->
                    match xs with 
                    | [] -> true
                    | xs ->
                        let lo = List.min xs
                        let hi = List.max xs
                        (Range.ofSeq xs) = (Range.ofBounds lo hi)

            testProp "range intersects itself "
                <| fun (r: 't Range) ->
                    Range.intersects r r

            testProp "range intersects empty"
                <| fun (r: 't Range) ->
                    Range.intersects r Range.empty

            testProp "points dont intersect"
                <| fun (a:'t, b:'t) ->
                    let a' = Range.ofPoint a
                    let b' = Range.ofPoint b 
                    if a < b || b < a then
                        not (Range.intersects a' b')
                    else
                        true

            testProp "map id does nothing"
                <| fun (r: 't Range) ->
                    r
                    |> Range.map id
                    |> (=) r

            testProp "union of lo is same" 
                <| fun (r: 't NonEmptyRange) ->
                    let r = r.Range
                    r
                    |> Range.union (Range.ofPoint r.Lo)
                    |> (=) r                    

            testProp "union of hi is same" 
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    r
                    |> Range.union (Range.ofPoint r.Hi)
                    |> (=) r

            testProp "bisect by lower bound"
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    let struct(a,b) = Range.bisect r (!r.Lo)
                    (a = !r.Lo) && (b = r)
                        
            testProp "bisect by upper bound"
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    let struct(a,b) = Range.bisect r (!r.Hi)
                    (b = !r.Hi) && (a = r)

            testProp "bisecting by self is the bounds"
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    let struct(a,b) = Range.bisect r r
                    (a = !r.Lo) && (b = !r.Hi)


                    
        ]


    [<Tests>]
    let tests= 
        testList "logic" [
            make<DateTime>()
            make<Int16>()
            make<Int32>()
            make<Int64>()
            make<char>()
            make<float>()
            make<single>()
            make<BoundedInt>()
        ]                


module DeltaTests = 

    [<Tests>]
    let tests =

        let inline test point delta = 
            let range  = Range.ofSize delta point
            let point2 = point + delta
            let size   = Range.size range

            if point <= point2
            then size = delta
            else size = -delta

        testList "delta/create" [
            testProp "time"  test<DateTime, TimeSpan>
            testProp "int16" test<Int16, Int16>
            testProp "int32" test<Int32, Int32>
            testProp "int64" test<Int64, int64>
            testProp "custom" test<BoundedInt, BoundedInt>
        ]

module RelationTests = 

    let x = 1
    let o = 0

    let r (xs: int list) : int Range = 
        let lo = List.tryFindIndex ((=) x) xs
        let hi = List.tryFindIndexBack ((=) x) xs
        match (lo, hi) with
        | Some lo', Some hi' ->
            Range.ofBounds lo' hi'
        | _ -> Range.empty

    let test x y rel : Test = 
        let x = r x
        let y = r y
        let rel' = Range.relation x y
        testCase (string rel) 
        <| fun () ->
            Expect.equal rel' rel (sprintf "wrong relation %O vs %O" x y)

    let inverse : Test = 
        
        let inline test (a: int Range) (b: int Range) =
            let fwd = Range.relation a b
            let rev = Range.relation b a

            match (fwd, rev) with
            
            | RangeRelation.Starts, RangeRelation.StartedBy 
            | RangeRelation.Finishes, RangeRelation.FinishedBy
            | RangeRelation.Empty, RangeRelation.Empty 
            | RangeRelation.StartedBy, RangeRelation.Starts
            | RangeRelation.FinishedBy, RangeRelation.Finishes -> true

            | RangeRelation.Starts, _
            | RangeRelation.StartedBy, _
            | RangeRelation.FinishedBy, _
            | RangeRelation.Finishes, _
            | RangeRelation.Empty, _ -> false

            | _ -> true

        testProp "inverse" test

    let equal : Test =
        testProp "equals" <| fun (r : int Range) ->
            (Range.relation r r) = RangeRelation.Equal
            

    [<Tests>]
    let tests = 
        testList "relations" [
            equal

            test 
                [x;x;x;x;x;o;o;o]
                [o;o;o;o;x;x;x;o]
                RangeRelation.MeetsStart

            test 
                [o;o;o;x;x;x;x;o]
                [x;x;x;x;o;o;o;o]
                RangeRelation.MeetsEnd

            test 
                [x;x;x;x;o;o;o;o]
                [o;o;o;o;o;o;x;x]
                RangeRelation.Before

            test 
                [o;o;o;o;o;o;x;x]
                [x;x;x;x;o;o;o;o]
                RangeRelation.After

            test 
                [o;x;x;x;x;x;o;o]
                [o;o;x;x;o;o;o;o]
                RangeRelation.Contains

            test 
                [o;o;x;x;o;o;o;o]
                [o;x;x;x;x;o;o;o]
                RangeRelation.Within

            test 
                [o;x;o;o;o;o;o;o]
                [o;x;o;o;o;o;o;o]
                RangeRelation.Equal

            test 
                [x;x;x;o;o;o;o;o]
                [o;x;x;x;x;o;o;o]
                RangeRelation.OverlapsStart

            test 
                [o;x;x;x;x;o;o;o]
                [x;x;x;o;o;o;o;o]
                RangeRelation.OverlapsEnd

            test 
                [o;x;x;x;x;o;o;o]
                [o;x;x;x;x;x;o;o]
                RangeRelation.Starts

            test 
                [o;x;x;x;x;o;o;o]
                [o;x;x;o;o;o;o;o]
                RangeRelation.StartedBy

            test 
                [o;o;o;o;x;x;o;o]
                [o;x;x;x;x;x;o;o]
                RangeRelation.Finishes

            test 
                [o;x;x;x;x;x;x;o]
                [o;o;o;o;o;x;x;o]
                RangeRelation.FinishedBy

            test 
                [o;x;o;o;o;o;o;o]
                [o;o;o;o;o;o;o;o]
                RangeRelation.Empty

        ]

module InlineTests =

    [<Tests>]
    let tests =
        testList "inline" [

            testCase "addition works " <| fun () ->
                let a = 2.0 <=> 3.0
                let b = a + !0.5
                let c = 2.5 <=> 3.5
                Expect.equal b c ""

            testCase "subtraction works " <| fun () ->
                let a = 5.0 <=> -5.0
                let b = a - 0.5
                let c = -5.5 <=> 4.5
                Expect.equal b c ""

            testCase "multiplication works" <| fun () ->
                let a =
                    Range.ofSize 4 0

                let b =  a * 10

                Expect.equal b (0 <=> 40) ""

            testCase "divison works" <| fun () ->
                let a = 3 <=> 12
                let b = Range.map (fun x -> x / 3) a
                Expect.equal b (1 <=> 4) ""

        ]

