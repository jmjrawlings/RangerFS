#load "Range.fs"

open Ranger
open Ranger.Operators
open System

let a = 5 <=> 10
let b = 4 <~> 2
let c = DateTime.Now =+> TimeSpan.FromDays 7.
let d = Range.intersection a b
let e = c.Size()
Range.intersects a b
