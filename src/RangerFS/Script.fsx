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
let zz = Range.tryMap2 (/) !5 (Range.zero ())
let za = (10 <=> 20).Bisect(9)

(0 <=> 100).Clamp(110) // 100
(0 <=> 100).Clamp(80 <=> 120) // {80..100}
(0 <=> 100).Clamp(-100) // 0
