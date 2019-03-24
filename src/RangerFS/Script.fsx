#load "Range.fs"

open Ranger
open Ranger.Operators

let a = 5 <=> 10
let b = 4 <~> 2
let c = Range.intersection a b
Range.intersects a b
