(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../src/RangerFS"

(**
RangerFS in 5 minutes
========================

Firstly reference `Range.fs` and open the `Ranger` namespace.

*)
#load "Range.fs"
open System
open Ranger
open Ranger.Operators

(* 
A `Range<'t>` represents the (possibly empty) interval between a Lower Bound and an Upper Bound.
The available constructors ensure that the invariant of range.Lo <= range.Hi always holds.
**)
let r = (0 <=> 100) // {0..100}
r.Lo // 0
r.Hi // 100

(**
# Constructing Ranges

The primary constructor of `Range` is `Range.ofBounds` or its equivalent operator `<=>` which takes a lower and
upper bound and returns the `Range`.  
*)
Range.ofBounds 0 10 // {0..10}
Range.ofBounds 4.0 4.5 // {4.0..4.5}
10 <=> 20 // {10..20}

(**
Note that `Range.ofBounds` will return `Empty` if the lower bound is greater than the upper bound
*)
Range.ofBounds 100 0 = Range.empty // true

(**
Use `Range.of2` or its equivalent operator `<~>` for order agnostic creation
*)
Range.of2 10 0 // {0..10}
Range.ofTuple (4, 2) // {2..4}
100 <~> 0 // {0..100}

(** 
The special case where `range.Lo = range.Hi` is referred to as a `Singleton` and can be constructed by
`Range.singleton` or its equivalent operator `!` *)
Range.singleton 2.5 // {2.5}
!2.5 // {2.5}
Range.ofBounds 2.5 2.5 // {2.5}

(** We can also construct a `Range` from a sequence of points with `Range.ofSeq` *)
Range.ofSeq [ 7; 5; -2; -100; 50; 75] // {-2..100}
Range.ofSeq [ 'a'; 'b'; 'q'; 'z'; 'h'; 'j'] // {'a'..'z'}

(** 
Constructing a `Range` from an empty list results in the Empty Range. 
Also available through `Range.empty`, the empty range is a natural way to represent the results
of invalid operations such as the intersection of non-overlapping `Ranges`
*)
let r : int Range = Range.ofSeq [] // {}
Range.isEmpty Range.empty // true

(** It is often more convenient to construct a `Range` using a single bound and a size - 
this can be done with `Range.ofSize` or the equivalent operator =+> *)

0.1 |> Range.ofSize 0.5 // {0.1 .. 0.6}
15 |> Range.ofSize 10 // {15..25}
DateTime.Now =+> (TimeSpan.FromDays 7.) // The next week

(** The size of a range may retrieved with `Range.ofSize` or `range.Size()` *)
let aFortnight = DateTime(2019,1,1) =+> (TimeSpan.FromDays 7.)
Range.size aFortnight // 7 days

(** Several extension methods are included to assist in creation *)
(1).ToRange(100) // {1..100}
DateTime(1900,1,1).ToRangeOfSize(TimeSpan.FromDays 7.) // {1/1/1900..8/1/1900}
(4.5).ToRange() // {4.5}
(5).ToRangeOfSize(5) // {5..10}

(**
# Relations 

The primary method of comparing ranges is `Range.relation` which returns a `Relation`.  The members of `Range.Relation`.
`Range.relation` represent an exhaustive enumeration of the possible ways in which two ranges may be related.

You can read more at [Allen's Interval Algebra](https://en.wikipedia.org/wiki/Allen%27s_interval_algebra
*)

Range.relation (0 <=> 5) (6 <=> 8) // Relation.Before
Range.relation (6 <=> 8) (0 <=> 5) // Relation.After
Range.relation (0 <=> 3) (0 <=> 5) // Relation.Starts
Range.relation (0 <=> 5) (0 <=> 3) // Relation.StartedBy
Range.relation (4 <=> 5) (0 <=> 5) // Relation.Finishes
Range.relation (0 <=> 5) (4 <=> 5) // Relation.FinshedBy
Range.relation (0 <=> 5) (5 <=> 10) // Relation.MeetsStarts
Range.relation (5 <=> 10) (0 <=> 5) // Relation.MeetsEnd
Range.relation (3 <=> 6) (5 <=> 8) // Relation.OverlapsStart
Range.relation (5 <=> 8) (3 <=> 6) // Relation.OverlapsEnd
Range.relation (3 <=> 4) (0 <=> 5) // Relation.Within
Range.relation (0 <=> 5) (3 <=> 4) // Relation.Contains
Range.relation (0 <=> 5) Range.empty // Relation.Empty

(**
Relations may also be constructed and examined through `range.Relation(other)` and `range.HasRelation(relation, other)`.
These methods are overloaded for so you may compare against `Range<'t>` or just a point of `t`.
*)

(0 <=> 10).Relation(11) // Relation.Before
(0 <=> 10).Relation(12 <=> 16) // Relation.Before
(!100).Relation(200, inverse=true) // Relation.After
(!50.0).HasRelation(Relation.Starts, 50.0 <=> 51.5) // true

(**
# Set-Theoretic Operations

As a `Range` is an abstract view of a set of elements a lot of the operations on sets hold.
The most basic operation is `Range.union` which constructs a new range that convers both of the 
input ranges
*)

Range.union (0 <=> 10) (50 <=> 60) // {0 .. 60}
Range.union (45 <=> 55) (!50) // {45 <=> 55}
Range.union ('a' <=> 'e') (!'z') // {'a' .. 'z'}

(** 
This function is used internally for `Range.ofSeq`
*)
let ofSeq points =
    points 
    |> Seq.map Range.singleton
    |> Seq.fold Range.union Range.empty

[1; 5; -100; 2]
|> Seq.ofList
|> ofSeq // {-100..5}


(**
We can also determine the intersection of lack thereof between two ranges
*)

Range.intersect (0 <=> 100) (80 <=> 150) // {80..100}
Range.intersect (10.1 <=> 10.2) (!10.2) // {10.2}

Range.intersect (5 <=> 10) (20 <=> 25) // {}
Range.intersects (5 <=> 10) (20 <=> 25) // false
Range.disjoint (5 <=> 10) (20 <=> 25) // true

(**
A range may be split by another range using `Range.bisect` which returns the peices of the original range
that lie before and after the given argument
*)
Range.bisect (0 <=> 10) (4 <=> 6) // ({0..4}, {6..10})
Range.bisect (4.5 <=> 5.5) (!5.0) // ({4.5..5.0}, {5.0..5.5})
Range.bisect (!10) (!10) // ({10},{10})

(**
# Transformation

`Ranger` provides implementations of `map` and `bind` to allow easy transformation.  Note that
these function take 2 arguments for the lower and upper bounds respectively.

*)

(0, 10)
|> Range.ofTuple
|> Range.map (fun lo -> lo - 10) (fun hi -> hi + 20)
// {-10..30}

(**
To map the same function for both ends use `Range.map1`
*)

(5 <=> 10) |> Range.map1 (fun x -> x * 10) // {500..1000}

(**
As a `Range<'t>` is generic we can go happily transform between types
*)

0
|> Range.ofSize 14
|> Range.map1 float
|> Range.map1 TimeSpan.FromDays
|> Range.map1 ((+) DateTime.Today)
|> Range.map1 (fun date -> date.ToShortDateString())
// the next fortnight

(**
# Algebra 

`RangerFS` supports [arithmetic](https://en.wikipedia.org/wiki/Interval_arithmetic) for all major operators.
Binary operators are applied as:

```
op a b = unionMany 
    [op a.Lo b.Lo
     op a.Lo b.Hi
     op a.Hi b.Lo
     op a.Hi b.Hi]
```
*)

(0 <=> 10) + 10 // {10..20}
(0 <=> 10) + (10 <=> 20) // {0..30}
(2 <=> 4) / 2 // {1..2}
(3.5 <=> 4.5) / 0.5 // {0.7 .. 0.9}
(100 <=> 200) * (2 <=> 5) // {200..1000}
(2.5 <=> 3.5) * 2. // {5.0..7.0}
(0 <=> 10) - (0 <=> 10) // {-10..10}
abs (-100 <=> 100) // {100}
-(12.0 <=> 12.1) // {-12.1..-12.0}
(10 <=> 20) / 0 // DivideByZeroException

(**
Internally these binary operators are just applications of `Range.map2` 
*)

Range.map2 (+) (0 <=> 2) (4 <=> 6) // {4..8}

(**
# Other Functions
*)
Range.buffer 5 (20 <=> 25) // {15 .. 30}
Range.buffer 10 (!0) // {-10..10}
Range.symmetric 10 // {-10..10}
Range.iterate 0.1 (0.0 <=> 0.5) // [0.0; 0.1; 0.2; 0.3; 0.4; 0.5]
Range.partition 4.0 (0.0 <=> 8.0) // [{0..2},{2..4},{4..6},{6..8}]
(0.0 <=> 100.0).Sample() // A sample point
(0 <=> 100).Clamp(110) // 100
(0 <=> 100).Clamp(80 <=> 120) // {80..100}
(0 <=> 100).Clamp(-100) // 0
