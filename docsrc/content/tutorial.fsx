(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/RangerFS/net47"

(**
RangerFS - Intervals for .NET
========================

RangerFS is a library for creating, manipulating, and comparing numeric intervals.
The type `Range<t>` allows intervals to be defined over many types including:

- `int`
- `float`
- `in64`
- `DateTime`
- `TimeSpan`
- `char` 
- any other type implementing `IComparable`

The generic nature of the library is enabled by FSharp's type system but is 
designed with CSharp interoperability in mind to allow idomatic use from both languages. 

# A Range of Integers
-------
*)

(** The simplest    *)
(*** define-output:simple ***)
#r "RangerFS.dll"

open Ranger
open Ranger.Operators

let a = Range.ofBounds 0 10
let b = Range.ofPoint 10000000L
let c = Range.ofSeq [3.2m; 2.8m; 1.04m; -9.42m; ]
let d = DateTime.Now |> Range.ofSize (TimeSpan.FromHours 3.) 
let e = -2.5 <=> 2.5 
let f = 'a' <=> 'z'

printf 
  "a = %O\nb = %O\nc = %O\nd = %O\ne = %O\nf = %O"
  a b c d e f  
(*** include-output:simple ***)

(** Transforming Ranges *)
(*** define-output:a ***) 
let aFortnight : DateTime Range = 
  Range.ofBounds -7 7
  |> Range.map float 
  |> Range.map TimeSpan.FromDays
  |> Range.map ((+) DateTime.Today)

printf """
  Fortnight = %O
  Duration = %O
  """ aFortnight (Range.size aFortnight)

(*** include-output:a ***)

(**





*)
#r "RangerFS.dll"
open Ranger

