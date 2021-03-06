namespace RangerFS.Tests

open Expecto
open FsCheck
open System
open Ranger
open Ranger.Operators

open Prelude

#nowarn "0686"

module ConstructionTests =

    let ofType<'t when 't:comparison> name : Test = 
            
      testList name [
        
          testProp "creation is order agnostic"
            <| fun (lo: 't, hi: 't) ->
                (Range.of2 lo hi) = (Range.of2 hi lo)

          testProp "ofPoint x = ofBounds x x"
            <| fun (x: 't) ->
                let r1 = Range.of2 x x
                let r2 = Range.singleton x
                r1 = r2 && r1.IsSingleton && r2.IsSingleton

          testProp "ofSeq works"
            <| fun (xs: 't list) ->
                let r = Range.ofSeq xs
                match xs with
                | [] -> r.IsEmpty
                | _  -> not r.IsEmpty

          testProp "ofBounds works"
              <| fun (lo: 't, hi: 't) ->
                  if lo > hi then
                    Range.ofBounds lo hi = Range.empty
                  else
                    true
      ]


    [<Tests>]
    let tests = 
        testList "creation" [
            ofType<DateTime> "time"
            ofType<Int16> "int16"
            ofType<Int32> "int32"
            ofType<Int64> "int64"
            ofType<char> "char"
            ofType<float> "float"
            ofType<float32> "float32"
            ofType<BoundedInt> "custom"
            ofType<int<kilometre>> "uom"
        ]        


module LogicTests = 

    let private ofType<'t when 't:comparison> name : Test =
            
        testList name [

            testProp "union with self is self"
                <| fun (x: 't Range) ->
                Range.union x x = x

            testProp "union is commutative"
                <| fun (a: 't Range, b: 't Range) ->
                (Range.union a b) = (Range.union b a)

            testProp "disjoint opposite of intersect"
                <| fun (a: 't Range, b: 't Range) ->
                (Range.intersects a b) <> (Range.disjoint a b)

            testProp "union many works" 
                <| fun (xs: List<'t>) ->
                    match xs with 
                    | [] -> true
                    | xs ->
                        let lo = List.min xs
                        let hi = List.max xs
                        (Range.ofSeq xs) = (Range.of2 lo hi)

            testProp "range intersects itself "
                <| fun (r: 't Range) ->
                    Range.intersects r r

            testProp "range intersects empty"
                <| fun (r: 't Range) ->
                    Range.intersects r Range.empty
                    &&
                    Range.intersects Range.empty r

            testProp "points dont intersect"
                <| fun (a:'t, b:'t) ->
                    let a' = Range.singleton a
                    let b' = Range.singleton b 
                    if a < b || b < a then
                        not (Range.intersects a' b')
                    else
                        true

            testProp "map id = id"
                <| fun (r: 't Range) ->
                    r
                    |> Range.map1 id
                    |> (=) r

            testProp "union of lo is same" 
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    r.Union(!r.Lo).Equals(r)

            testProp "union of hi is same" 
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    r.Union(!r.Hi).Equals(r)

            testProp "bisect by lower bound"
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    let (a,b) = r.Bisect(r.Lo)
                    (a = !r.Lo) && (b = r)
                        
            testProp "bisect by upper bound"
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    let (a,b) = r.Bisect(r.Hi)
                    (b = !r.Hi) && (a = r)

            testProp "bisecting by self is the bounds"
                <| fun ({Range=r}: 't NonEmptyRange) ->
                    let (a,b) = Range.bisect r r
                    (a = !r.Lo) && (b = !r.Hi)

            testProp "bisecting is clamped"
                <| fun (a: 't Range, b: 't Range) ->
                    let (before,after) = Range.bisect a b
                    before.Within(a) && after.Within(a)

            testProp "tryMap catches"
                <| fun (a: 't Range) ->
                    let trial x = failwith "exception"
                    a
                    |> Range.tryMap trial trial
                    |> Range.isEmpty
        ]

    [<Tests>]
    let tests= 
        testList "logic" [
            ofType<DateTime> "date"
            ofType<Int16> "int16"
            ofType<Int32> "int32"
            ofType<Int64> "int64"
            ofType<char> "char"
            ofType<float> "float"
            ofType<single> "single"
            ofType<BoundedInt> "custom"
            ofType<int<kilometre>> "uom"
        ]                


module DeltaTests = 

    [<Tests>]
    let tests =

        let inline ofType point delta = 
            let range  = Range.ofSize delta point
            let point2 = point + delta
            let size   = Range.size range

            if point <= point2
            then size = delta
            else size = -delta

        testList "delta/create" [
            testProp "time"    ofType<DateTime, TimeSpan>
            testProp "int16"   ofType<Int16, Int16>
            testProp "int32"   ofType<Int32, Int32>
            testProp "int64"   ofType<Int64, int64>
            testProp "measure" ofType<int<kilometre>, int<kilometre>>
        ]

module RelationTests = 

    let private x = 1
    let private o = 0

    let private makeRangeFromASCII (xs: int list) : int Range = 
        let lo = List.tryFindIndex ((=) x) xs
        let hi = List.tryFindIndexBack ((=) x) xs
        match (lo, hi) with
        | Some lo', Some hi' ->
            Range.of2 lo' hi'
        | _ -> Range.empty

    let private makeTest x y (expected:Relation) : Test = 
        let x = makeRangeFromASCII x
        let y = makeRangeFromASCII y
        let actual: Relation = Range.relation x y

        testCase (string expected) <| fun () ->
            Expect.equal actual expected (sprintf "relation test %O vs %O" x y)

    let inverseTest : Test = 

        let inverseRelations = 
            [
             Relation.Starts, Relation.StartedBy 
             Relation.Finishes, Relation.FinishedBy
             Relation.Before, Relation.After
             Relation.Empty, Relation.Empty 
             Relation.MeetsStart, Relation.MeetsEnd
             Relation.Within, Relation.Contains
             Relation.OverlapsStart, Relation.OverlapsEnd
             Relation.Equal, Relation.Equal
             Relation.Empty, Relation.Empty
            ]
            |> Set.ofSeq

        let inline test (a: int Range) (b: int Range) =
            let fwd = Range.relation a b
            let rev = Range.relation b a
            inverseRelations.Contains (fwd, rev)
            ||
            inverseRelations.Contains (rev, fwd)

        testProp "inverse" test
    

    let equalityTest : Test =
        testProp "equals" <| fun (r : int Range) ->
            (Range.relation r r) = Relation.Equal


    [<Tests>]
    let tests = 
        testList "relations" [
            equalityTest

            inverseTest

            makeTest
                [x;x;x;x;x;o;o;o]
                [o;o;o;o;x;x;x;o]
                Relation.MeetsStart

            makeTest
                [o;o;o;x;x;x;x;o]
                [x;x;x;x;o;o;o;o]
                Relation.MeetsEnd

            makeTest
                [x;x;x;x;o;o;o;o]
                [o;o;o;o;o;o;x;x]
                Relation.Before

            makeTest
                [o;o;o;o;o;o;x;x]
                [x;x;x;x;o;o;o;o]
                Relation.After

            makeTest
                [o;x;x;x;x;x;o;o]
                [o;o;x;x;o;o;o;o]
                Relation.Contains

            makeTest
                [o;o;x;x;o;o;o;o]
                [o;x;x;x;x;o;o;o]
                Relation.Within

            makeTest
                [o;x;o;o;o;o;o;o]
                [o;x;o;o;o;o;o;o]
                Relation.Equal

            makeTest
                [x;x;x;o;o;o;o;o]
                [o;x;x;x;x;o;o;o]
                Relation.OverlapsStart

            makeTest
                [o;x;x;x;x;o;o;o]
                [x;x;x;o;o;o;o;o]
                Relation.OverlapsEnd

            makeTest
                [o;x;x;x;x;o;o;o]
                [o;x;x;x;x;x;o;o]
                Relation.Starts

            makeTest
                [o;x;x;x;x;o;o;o]
                [o;x;x;o;o;o;o;o]
                Relation.StartedBy

            makeTest
                [o;o;o;o;x;x;o;o]
                [o;x;x;x;x;x;o;o]
                Relation.Finishes

            makeTest
                [o;x;x;x;x;x;x;o]
                [o;o;o;o;o;x;x;o]
                Relation.FinishedBy

            makeTest
                [o;x;o;o;o;o;o;o]
                [o;o;o;o;o;o;o;o]
                Relation.Empty

        ]

module InlineTests =

    [<Tests>]
    let tests =
        testList "inline" [

            testCase "addition" <| fun () ->
                let a = 2.0 <=> 3.0
                let b = a + 0.5
                let c = 2.5 <=> 3.5
                Expect.equal b c ""

            testCase "subtraction" <| fun () ->
                let a = 5.0 <~> -5.0
                let b = a - 0.5
                let c = -5.5 <=> 4.5
                Expect.equal b c ""

            testCase "multiplication" <| fun () ->
                let a = -4 <=> 4
                let b = a * 4
                Expect.equal b (-16 <=> 16) ""

            testCase "divison" <| fun () ->
                let a = 3 <=> 12
                let b = a / 3
                Expect.equal b (1 <=> 4) ""

            testCase "modulo" <| fun () ->
                let a = 3 <=> 12
                let b = a % 3
                Expect.equal b !0 ""

        ]
