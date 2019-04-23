#load "Range.fs"

open Ranger
open Ranger.Operators
open System

let a = 5 <=> 10
let b = 0 <=> 100
let c = 10 =+> 100 
let d = Range.intersect a b
let e = Range.symmetric Math.PI
let f = DateTime.Today =+> (TimeSpan.FromDays 31.)
let (before,after) = (10 <=> 20).Bisect(15)
let clamped = (0 <=> 100).Clamp(110) // 100
