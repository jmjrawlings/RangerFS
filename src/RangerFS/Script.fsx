#load "Range.fs"

open Ranger
open Ranger.Operators

let a = 5 <=> 10
let b = 2 <=> 4
let c = Range.haursoff b a
Range.ofSymmetric 100
