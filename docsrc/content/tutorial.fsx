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
let c = Range.ofBounds 2 10
let d = Range.ofBounds 10 2

printf "%O" (c = d)
(*** include-output:b ***)

(** The special case where `range.Lo = range.Hi` is referred to as a `Point` and can be constructed by
`Range.ofPoint` or it's operator equivalent `!` *)
(*** define-output:c ***)
// The following are all equivalent
let e = Range.ofPoint 2.5
let f = !2.5
let g = Range.ofBounds 2.5 2.5
let h = (2.5).ToRange()

printf "%O = %O = %O = %O" e f g h
(*** include-output:c ***)

(** We can also construct a `Range` from a sequence of points with `Range.ofSeq` *)
let i = Range.ofSeq [ 7; 5; -2; -100; 50; 75]
(*** include-value:h ***)

let j = Range.ofSeq [ 'a'; 'b'; 'q'; 'z'; 'h'; 'j']

(*** include-value:i ***)

(** `Ranges` of a certain magnitude may be constructed using `Range.ofSize` **)
let k = 0.1 |> Range.ofSize 0.5 // {0.1 .. 0.6}
let l = 
    DateTime.Today
    |> Range.ofSize (TimeSpan.FromDays 14.) // The next fortnight

let fourteenDays = Range.size l

