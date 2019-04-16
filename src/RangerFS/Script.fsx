#load "Range.fs"

open Ranger
open Ranger.Operators
open System

let a = 5 <=> 10
let b = 0 <=> 100
let c = Range.clamp a b
let q = 10 =+> 100 
let d = Range.intersect a b
let e = Range.symmetric Math.PI
let zz = Range.tryMap2 (/) !5 (Range.zero ())
let za = (10 <=> 20).Bisect(11)