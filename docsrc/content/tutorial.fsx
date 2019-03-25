(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/RangerFS/net451"

(**
RangerFS in 5 minutes
========================

Firstly reference `RangerFS.dll` from [Nuget]("https://nuget.org/packages/RangerFS") and then reference
the `.dll` in F# Interactive and open the `Ranger` namespace.

*)
#r "RangerFS.dll"
open System
open Ranger
open Ranger.Operators

(**
# Constructing Ranges

The primary constructor of `Range<'t>` is `Range.ofBounds` or the equivalent operator `<=>`
*)

(*** define-output:a ***)
let a = Range.ofBounds 0 10
let b = 0 <=> 10

printf "%O is equal to %O" a b
(*** include-output:a ***)

(** Arguments to `Range.ofBounds` may be provided in any order *)
(*** define-output:b ***)
let c = Range.ofBounds 2 10 // {2..10}
let d = Range.ofBounds 10 2 // {2..10}

(** The special case where `range.Lo = range.Hi` is referred to as a `Point` and can be constructed by
`Range.ofPoint` or it's operator equivalent `!` *)
(*** define-output:c ***)
// The following are all equivalent
let e = Range.ofPoint 2.5 // {2.5}
let f = !2.5 // {2.5}
let g = Range.ofBounds 2.5 2.5 // {2.5}
let h = (2.5).ToRange() // {2.5}

(** We can also construct a `Range` from a sequence of points with `Range.ofSeq` *)
let seq1 = Range.ofSeq [ 7; 5; -2; -100; 50; 75]
(*** include-value:i ***)

let seq2 = Range.ofSeq [ 'a'; 'b'; 'q'; 'z'; 'h'; 'j']
(*** include-value:j ***)

(** 
Constructing a `Range` from an empty list results in the Empty Range. 
Also available through `Range.empty`, the empty range is a natural way to represent the results
of invalid operations such as the intersection of non-overlapping `Ranges`
*)

let seq3 : int Range = Range.ofSeq [] 
let seq4 = Seq.empty |> Range.ofSeq |> Range.isEmpty // true

(** `Ranges` of a certain magnitude may be constructed using `Range.ofSize` **)
let k = 0.1 |> Range.ofSize 0.5 // {0.1 .. 0.6}
let aFortnight = 
    DateTime.Today
    |> Range.ofSize (TimeSpan.FromDays 14.) // The next fortnight

(** The size of a range may retrieved with `Range.ofSize` **)
let fourteenDays = Range.size aFortnight
(*** include-value:fourteenDays ***)

(**
# Relations 

The primary method of comparing `Ranges` is `Range.relation` which returns a `Relation`.  The members of `Range.Relation`.
`Range.relation` represent a complete enumeration of the possible ways in which two `Ranges` may be related.

You can read more at [Allen's Interval Algebra](https://en.wikipedia.org/wiki/Allen%27s_interval_algebra
*)

let r1 = Range.relation (0 <=> 5) (6 <=> 8) // Relation.Before
let r2 = Range.relation (6 <=> 8) (0 <=> 5) // Relation.After
let r3 = Range.relation (0 <=> 3) (0 <=> 5) // Relation.Starts
let r4 = Range.relation (0 <=> 5) (0 <=> 3) // Relation.StartedBy
let r5 = Range.relation (4 <=> 5) (0 <=> 5) // Relation.Finishes
let r6 = Range.relation (0 <=> 5) (4 <=> 5) // Relation.FinshedBy
let r7 = Range.relation (0 <=> 5) (5 <=> 10) // Relation.MeetsStarts
let r8 = Range.relation (5 <=> 10) (0 <=> 5) // Relation.MeetsEnd
let r9 = Range.relation (3 <=> 6) (5 <=> 8) // Relation.OverlapsStart
let r10 = Range.relation (5 <=> 8) (3 <=> 6) // Relation.OverlapsEnd
let r11 = Range.relation (3 <=> 4) (0 <=> 5) // Relation.Within
let r12 = Range.relation (0 <=> 5) (3 <=> 4) // Relation.Contains
let r13 = Range.relation (0 <=> 5) Range.empty // Relation.Empty

(**
Relations may also be constructed and examined through `range.Relation(other)` and `range.HasRelation(relation, other)`.
These methods are overloaded for so you may compare against `Range<'t>` or just a point of `t`.
*)

let rr1 = (0 <=> 10).Relation(11) // Relation.Before
let rr2 = (0 <=> 10).Relation(12 <=> 16) // Relation.Before
let rr3 = (100).ToRange().Relation(200, inverse=true) // Relation.After
let rr4 = (!50.0).HasRelation(Relation.Starts, 50.0 <=> 51.5) // true

(**
# Algebra

`RangerFS` supports all major operators.  Binary operators are implemented as the `union` of the
results of applying the operator to each pair of bounds:

`op r1 r2 = unionMany [op r1.Lo r2.Lo; op r1.Lo r2.Hi; op r1.Hi r2.Lo; op r1.Hi r2.Hi]`
*)

let op1 = !2 + !2 // {4}
let op2 = (0 <=> 10) + (10 <=> 20) // {0..30}
let op3 = (2 <=> 4) / 2 // {1..2}
let op4 = (3.5 <=> 4.5) // 0.5 // {0.7 .. 0.9}
let op5 = (100 <=> 200) * (2 <=> 5) // {200..1000}
let op6 = (0 <=> 10) - (0 <=> 10) // {0..10}
let op7 = abs (-100 <=> 100) // {100}
let op8 = -(12.0 <=> 12.1) // {-12.1..12.0}

(**
# Other Functions
*)

let o1 = Range.buffer 5 (20 <=> 25) // {15 .. 30}
let o2 = Range.ofSymmetric 3.1 // {-3.1 .. 3.1}
let o3 = Range.step 0.1 (0.0 <=> 0.5) // [0.0; 0.1; 0.2; 0.3; 0.4; 0.5]
let o4 = Range.bisect (0 <=> 10) (!6) // {0..6}, {6..10}
let o5 = Range.bisect (10 <=> 90) (20 <=> 25) // {10..20}, {25..90}
let o6 = Range.partition 4.0 (0.0 <=> 8.0) // [{0..2},{2..4},{4..6},{6..8}]
