(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/RangerFS/net47"

(**
RangerFS in 5 minutes
========================

Firstly reference `RangerFS.dll` from [Nuget]("https://nuget.org/packages/RangerFS") and then reference
the `.dll` in F# Interactive and open the `Ranger` namespace.

*)
#r "RangerFS.dll"

open Ranger
open Ranger.Operators

(*
# A Simple Range

The primary constructor of `Range<'t>` is `Range.ofBounds` or it's operator equivalent `<=>`
*)

(** define-output:a *)
let a = Range.ofBounds 0 10
let b = 0 <=> 10

printf "%O is equal to %O" a b
(*** include-output:a ***)

(** Arguments to `Range.ofBounds` may be provided in any order *)
(** define-output:b *)
let c = Range.ofBounds 2 10
let d = Range.ofBounds 10 2

printf "%O" (b = c)
(*** include-output:b ***)

(** The special case where `range.Lo = range.Hi` can be constructed by
`Range.ofPoint` or it's operator equivalent `!` *)

(** define-output:c *)
let e = Range.ofPoint 2.5
let f = Range.ofBounds 2.5 2.5
let g = !2.5

printf "%O = %O = %O" e f g 
(*** include-output:c ***)

(** We can also construct a `Range` from a sequence of points with `Range.ofSeq` *)
(** define-output:d *)
Range.ofSeq [ 7; 5; -2; -100; 50; 75]
(*** include-output:d ***)
