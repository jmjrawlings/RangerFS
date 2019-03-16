#load "Range.fs"
open Ranger
open Ranger.Operators

let a = (0 <=> 10)
let b = -a
let c = -b
let d= Range.union a b
