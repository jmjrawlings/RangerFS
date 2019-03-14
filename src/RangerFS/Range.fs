namespace rec Ranger

open System
open FSharp.Core
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

/// For operations performed on the Empty Range
exception EmptyRangeException
    
type IRangeProvider<'t when 't: comparison> = 
    abstract member Range : 't Range

[<Struct>]
[<NoComparison>]
[<StructuredFormatDisplay("{Description}")>]
/// A Range representing the values between a Lower and Upper Bound
type Range<'t when 't : comparison> = 
    internal
    | Empty_
    | Point_ of 't
    | Range_ of a:'t * b: 't    

    /// The lower bound of the Range
    member this.Lo : 't = Range.lo this

    /// The upper bound of the Range
    member this.Hi : 't = Range.hi this

    /// Returns true if the given range is the Empty Range
    member this.IsEmpty = Range.isEmpty this

    /// Returns true if the Range contains a single value
    member this.IsPoint = Range.isPoint this

    override this.ToString() =
        Range.show this

    member this.Description = Range.show this

    interface IRangeProvider<'t> with
        member this.Range = this


// And Item and its associated Range
type TaggedRange<'bound,'item when 'bound:comparison> = 
    { Item: 'item; Range : 'bound Range }

    interface IRangeProvider<'bound> with
        member __.Range = __.Range


/// Allen's Interval Algebra relation table
/// https://en.wikipedia.org/wiki/Allen%27s_interval_algebra#Composition_of_relations_between_intervals
type RangeRelation = 
    /// One or both the ranges are empty
    | Empty     

    /// X: ---
    /// Y:     ---
    | Before    

    /// X:     ---
    /// Y: ---
    | After     

    /// X: ---
    /// Y: -------
    | Starts    

    /// X: -------
    /// Y: ---
    | StartedBy 
    
    /// X:     ---
    /// Y: -------
    | Finishes  

    /// X: -------
    /// Y:     ---
    | FinishedBy 

    /// X: ---
    /// Y:    ----
    | MeetsStart 

    /// X:    ----
    /// Y: ---
    | MeetsEnd 

    /// X: -----
    /// Y:    ----
    | OverlapsStart 

    /// X: -----
    /// Y:    ----
    | OverlapsEnd 

    /// X:  ---
    /// Y: -------
    | Within 

    /// X: -------
    /// Y:   ---
    | Contains 

    /// X: -------
    /// Y: -------
    | Equal 

module Operators = 
    let (<=>) a b = Range.ofBounds a b
    let (<~>) a b = Range.ofOrdered a b
    let (!) p     = Range.ofPoint p
    let (++) a b  = Range.union a b
    let (--) a b  = Range.difference a b

module private Compare = 
    
    [<Struct>]
    type Comparison =
        | LT 
        | GT 
        | EQ 

    let inline max a b =
        if a > b then a else b

    let inline min a b =
        if a < b then a else b

    let inline cmp a b : Comparison = 
        let c = compare a b
        if      c < 0 then LT
        else if c > 0 then GT
        else    EQ


[<RequireQualifiedAccess>]    
module Range = 

    open Compare
    open Operators

    /// ToString
    let show (r: 't Range) : string =
        match r with 
        | Empty_         -> "{}"
        | Point_ p       -> sprintf "{%O}" p
        | Range_ (lo,hi) -> sprintf "{%O .. %O}" lo hi

    /// Return the low value of the Range 
    let lo (r: 't Range) : 't =
        match r with
        | Range_ (x, _) 
        | Point_ x -> x
        | Empty_ -> raise EmptyRangeException

    /// Return the high value of the range
    let hi (r: 't Range) : 't =
        match r with
        | Range_ (_, x) 
        | Point_ x -> x
        | Empty_ -> raise EmptyRangeException

    /// Return the size of the Range
    [<CompiledName("Size")>]
    let inline size (r: ^t Range) : ^u =
        r.Hi - r.Lo

    /// Returns true if the given range is Empty
    let isEmpty (r: 't Range) : bool = 
        r = Empty_

    /// Returns true if the bounds are equals
    [<CompiledName("IsPoint")>]
    let isPoint (r: 't Range) : bool = 
        match r with
        | Point_ _ -> true 
        | _ -> false

    /// The Empty Range
    [<CompiledName("Empty")>]
    let empty : 't Range = Empty_

    /// Construct a Range of a single value
    [<CompiledName("Create")>]
    let ofPoint (p: 't) : 't Range =
        Point_ p

    /// Construct a Range that spans the given bounds
    [<CompiledName("Create")>]
    let ofBounds (lo: 't) (hi: 't) : 't Range =
        match cmp lo hi with
        | LT -> Range_ (lo, hi)
        | EQ -> Point_ lo
        | GT -> Range_ (hi, lo)

    /// Construct a Range from the given Tuple
    [<CompiledName("Create")>]
    let ofTuple struct (lo, hi) : 't Range = 
        ofBounds lo hi

    /// Construct a Range that is non empty only if lo <= hi
    let internal ofOrdered (lo: 't) (hi: 't) : 't Range =        
        match cmp lo hi with
        | LT -> Range_ (lo, hi)
        | EQ -> Point_ lo
        | GT -> Empty_

    /// Construct a Range with a single bound and a size
    [<CompiledName("OfSize")>]
    let inline ofSize (delta: ^d) (p: ^t)  : ^t Range = 
        p <=> (p + delta)

    /// Construct a Range from a sequence of comparable items
    [<CompiledName("Create")>]
    let ofSeq(xs: #seq<'t>) : 't Range =
        xs 
        |> Seq.map ofPoint
        |> unionMany

    /// Returns the Zero range
    [<CompiledName("Zero")>]
    let inline zero () = 
        ofPoint LanguagePrimitives.GenericZero

    /// Returns the relation from x to y
    [<CompiledName("Relation")>]
    let relation (x: 't Range) (y: 't Range) : RangeRelation =    
        match (x, y) with
        | Empty_, Empty_ -> RangeRelation.Equal
        | Empty_, _ -> RangeRelation.Empty
        | _, Empty_ -> RangeRelation.Empty
        | _, _->
            match (cmp x.Lo y.Lo, cmp x.Hi y.Hi) with
            
            | EQ, EQ -> RangeRelation.Equal
            | EQ, LT -> RangeRelation.Starts
            | EQ, GT -> RangeRelation.StartedBy
            | GT, EQ -> RangeRelation.Finishes
            | LT, EQ -> RangeRelation.FinishedBy
            | LT, GT -> RangeRelation.Contains
            | GT, LT -> RangeRelation.Within

            | LT, LT when x.Hi = y.Lo -> RangeRelation.MeetsStart
            | LT, LT when x.Hi > y.Lo -> RangeRelation.OverlapsStart
            | LT, LT -> RangeRelation.Before

            | GT, GT when x.Lo = y.Hi -> RangeRelation.MeetsEnd
            | GT, GT when x.Lo < y.Hi -> RangeRelation.OverlapsEnd
            | GT, GT -> RangeRelation.After 

    /// Returns the union of two Ranges
    [<CompiledName("Union")>]
    let union (a: 't Range) (b: 't Range) : 't Range =
        if      a.IsEmpty then b
        else if b.IsEmpty then a
        else 
            let lo = min a.Lo b.Lo
            let hi = max a.Hi b.Hi
            lo <=> hi

    /// Returns the range a without the elements of range b
    [<CompiledName("Difference")>]
    let difference (a: 't Range) (b: 't Range) : 't Range =
        if (a.IsEmpty || b.IsEmpty)
        then a
        else 
            let lo = min a.Lo b.Lo
            let hi = max a.Hi b.Hi
            lo <=> hi

    /// Returns the union of many ranges
    [<CompiledName("UnionMany")>]
    let unionMany (xs: 't Range seq) : 't Range = 
        Seq.fold union empty xs

    /// Returns the intersection of the given ranges
    [<CompiledName("Intersection")>]
    let intersection (a: 't Range) (b: 't Range) : 't Range = 
        if a.IsEmpty || b.IsEmpty 
        then empty
        else 
            let lo = max a.Lo b.Lo
            let hi = min a.Hi b.Hi
            lo <~> hi

    /// Returns true if b does not exceed the bounds of a
    [<CompiledName("Contains")>]
    let contains (a: 't Range) (b: 't Range) : bool =
        match relation a b with
        | RangeRelation.Before 
        | RangeRelation.After 
        | RangeRelation.MeetsStart
        | RangeRelation.MeetsEnd -> false
        | RangeRelation.Empty -> a.IsEmpty
        | _ -> true

    /// Returns true if a does not exceed the bounds of b
    [<CompiledName("Within")>]
    let within (a: 't Range) (b: 't Range) : bool = 
        not (contains b a)
        
    /// Returns true if the given Range intersect
    [<CompiledName("Intersects")>]
    let intersects (a: 't Range) (b: 't Range) : bool =
        match relation a b with 
        | RangeRelation.After 
        | RangeRelation.Before -> false
        | _ -> true

    /// Returns true of the given Ranges do not intersect
    [<CompiledName("Disjoint")>]
    let disjoint (a: 't Range) (b: 't Range) : bool =
        not (intersects a b)

    /// Apply the given functions to the lower and upper bounds of the Range
    let map2 (fLo: 't -> 'u) (fHi: 't -> 'u) (r: 't Range) : 'u Range =
        match r with
        | Empty_ -> Empty_
        | Point_ p -> (fLo p) <=> (fHi p)
        | Range_ (lo, hi) -> (fLo lo) <=> (fHi hi)

    /// Apply the given function to the lower and upper bounds of the Range
    let map f r = map2 f f r

    /// Standard bind operator
    let bind2 (fLo: 't -> 'u Range) (fHi: 't -> 'u Range) (r: 't Range) : 'u Range =
        if r.IsEmpty 
        then Empty_
        else (fLo r.Lo) ++ (fHi r.Hi)

    /// Standard bind operator
    let bind f r = bind2 f f r
        
    /// Apply the given function to the Lo value of the Range
    let mapLo (f: 't -> 't) : Range<'t> -> Range<'t> =
         map2 f id

    /// Apply the given function to the Hi value of the Range
    let mapHi (f: 't -> 't) : Range<'t> -> Range<'t> =
        map2 id f

    /// Combine an element with a Range
    let tag (item: 'u) (rng: 't Range) : TaggedRange<'t, 'u> = 
        { Item = item; Range = rng }

    /// Bisect the range returning the range before and after the given point
    /// bisect {0,10} {5}   = {0,5}, {5,10}
    /// bisect {0,10} {2,4} = {0,2}, {4,10}
    /// bisect {3}  {3}     = {3},{3}
    /// bisect {0.0, 1.0} {-3.0} = {}, {0.0,1.0}
    /// bisect {}, {'a','z'} = {}
    [<CompiledName("Bisect")>]
    let bisect (a: 't Range) (b: 't Range) : struct ('t Range * 't Range) =
        if a.IsEmpty || b.IsEmpty then
            struct (empty, empty)
        else
            let before = a.Lo <~> b.Lo
            let after  = b.Hi <~> a.Hi
            struct (before, after)

    /// Buffer the range by the given delta
    /// Buffer 5 {0} = {-5, 5}
    [<CompiledName("Buffer")>]
    let inline buffer (delta: ^u) (r: ^t Range) : ^t Range =
        map2
            (fun p -> p - delta)
            (fun p -> p + delta)
            r

    [<RequireQualifiedAccess>]
    module Sample =

        let private random = 
            Lazy<System.Random>.Create 
            <| fun _ -> System.Random()

        /// Return a random long
        let private randomLong min max : int64 =
            let buffer : byte[] = Array.zeroCreate 8
            random.Value.NextBytes(buffer);
            let longRand = BitConverter.ToInt64(buffer, 0)
            Math.Abs (longRand % (max - min)) + min

        /// Sample a random point from the Range
        let int (rng: int Range) : int =
            random.Value.Next(rng.Lo, rng.Hi)
            
        /// Sample a random point from the Range
        let float (rng: float Range) : float =
            rng.Lo + (random.Value.NextDouble() * size rng)

        /// Sample a random point from the Range
        let time (rng: DateTime Range) : DateTime =
            let span : TimeSpan = size rng
            rng.Lo.AddTicks (randomLong 0L span.Ticks)

        /// Sample a random point from the Range
        let long (rng: int64 Range) : int64 =
            randomLong rng.Lo rng.Hi

    /// Combine the two ranges by cross-applying the function between all the bounds
    /// combine (+) {1,2} {3,4} = {3, 6}
    let combine (f: 't -> 't -> 'u) (a: 't Range) (b: 't Range) : 'u Range =
        a |> bind (fun a' -> 
            b |> map (fun b' -> f a' b'))

    let inline add a b = combine (+) a b
    let inline sub a b = combine (-) a b
    let inline mul a b = combine (*) a b
    let inline div a b = combine (/) a b


/// Operator support
type Range<'t when 't:comparison> with

    static member inline (+) (a, b) =
       Range.add a b

    static member inline (+) (a, b) =
       Range.add a (Range.ofPoint b)

    static member inline (-) (a, b) =
       Range.sub a b

    static member inline (-) (a, b) =
       Range.sub a (Range.ofPoint b)

    static member inline (*) (a, b) =
       Range.mul a b

    static member inline (*) (a, b) =
       Range.mul a (Range.ofPoint b)

    static member inline (/) (a, b) =
       Range.div a b

    static member inline (/) (a, b) =
       Range.div a (Range.ofPoint b)
    
    /// Map the given function to both bounds of the range
    member this.Map(range: 't Range, f: Func<'t,'u>) : 'u Range = 
       let f' = FuncConvert.FromFunc f
       Range.map f' range
    
    /// Map the given functions to the bounds of the range
    member this.Map(range: 't Range, fLo: Func<'t,'u>, fHi: Func<'t,'u>) = 
       Range.map2 (FuncConvert.FromFunc fLo) (FuncConvert.FromFunc fHi) range
    
    /// Bind the given functions to the bounds of the range
    member this.Bind(range: 't Range, fLo: Func<'t,'u Range>, fHi: Func<'t,'u Range>) = 
       Range.bind2 (FuncConvert.FromFunc fLo) (FuncConvert.FromFunc fHi) range
    
    /// Bind the given functions to the bounds of the range
    member this.Bind(range: 't Range, f: Func<'t,'u Range>) = 
       Range.bind (FuncConvert.FromFunc f) range
    
    /// Returns the union of the range with the given point
    member this.Union(range: 't Range, point: 't) : 't Range = 
       Range.union range (Range.ofPoint point)
    
    /// Returns the union of the two ranges
    member this.Union(a: 't Range, b: 't Range) : 't Range = 
       Range.union a b
    
    // Returns the union the range with the given ofPoints
    member this.Union(range: 't Range, points: 't seq) : 't Range = 
       points
       |> Range.ofSeq
       |> Range.union range   
    
    /// Bisect the Range at the given point
    member this.Bisect(that: 't Range) : struct('t Range * 't Range) =
       Range.bisect this that
    
    /// Bisect the Range at the given point
     member this.Bisect(range: 't Range, point:'t) : struct('t Range * 't Range) =
        Range.bisect range (Range.ofPoint point)    

    /// Returns the Relation from a -> b, or from b -> a if inverse is true
    member this.Relation(that: 't Range, [<Optional;DefaultParameterValue(false)>]inverse:bool) : RangeRelation = 
        if inverse then
            Range.relation this that
        else
            Range.relation that this

    /// Returns the Relation from a -> b, or from b -> a if inverse is true
    member this.Relation(b: 't, [<Optional;DefaultParameterValue(false)>]inverse:bool) : RangeRelation = 
        this.Relation(Range.ofPoint b, inverse)
    
    /// Check if the relationship holds between the two ranges
    member inline this.Is(relation:RangeRelation, that: 't Range) : bool =
       this.Relation(that) = relation
    
    /// Check if the relationship holds between the two ranges
    member inline this.Is(relation:RangeRelation, point: 't) : bool =
       this.Is(relation, Range.ofPoint point)
    
    /// Returns true if RangeRelation.Before holds
    member inline this.Before(that: 't Range) : bool =
       this.Is(RangeRelation.Before, that)
    
    /// Returns true if RangeRelation.After holds
     member inline this.After(that: 't Range) : bool =
        this.Is(RangeRelation.After, that)
    
    /// Returns true if RangeRelation.Starts holds
     member inline this.Starts(that: 't Range) : bool =
        this.Is(RangeRelation.Starts, that)
    
    /// Returns true if RangeRelation.StartedBy holds
     member inline this.StartedBy(that: 't Range) : bool =
        this.Is(RangeRelation.StartedBy, that)
    
    /// Returns true if RangeRelation.Finishes holds
     member inline this.Finishes(r, that: 't Range) : bool =
        this.Is(RangeRelation.Finishes, that)
    
    /// Returns true if RangeRelation.FinishedBy holds
     member inline this.FinishedBy(that: 't Range) : bool =
        this.Is(RangeRelation.FinishedBy, that)
    
    /// Returns true if RangeRelation.MeetsStart holds
     member inline this.MeetsStart(that: 't Range) : bool =
        this.Is(RangeRelation.MeetsStart, that)
    
    /// Returns true if RangeRelation.MeetsEnd holds
     member inline this.MeetsEnd(that: 't Range) : bool =
        this.Is(RangeRelation.MeetsEnd, that)
    
    /// Returns true if RangeRelation.OverlapsStart holds
     member inline this.OverlapsStart(that: 't Range) : bool =
        this.Is(RangeRelation.OverlapsStart, that)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.OverlapsEnd(that: 't Range) : bool =
        this.Is(RangeRelation.OverlapsEnd, that)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.Before(point: 't) : bool =
        this.Is(RangeRelation.Before, Range.ofPoint point)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.After(point: 't) : bool =
        this.Is(RangeRelation.After, Range.ofPoint point)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.Starts(point: 't) : bool =
        this.Is(RangeRelation.Starts, Range.ofPoint point)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.StartedBy(point: 't) : bool =
        this.Is(RangeRelation.StartedBy, Range.ofPoint point)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.Finishes(point: 't) : bool =
        this.Is(RangeRelation.Finishes, Range.ofPoint point)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.FinishedBy(point: 't) : bool =
        this.Is(RangeRelation.FinishedBy, Range.ofPoint point)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.MeetsStart(point: 't) : bool =
        this.Is(RangeRelation.MeetsStart, Range.ofPoint point)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.MeetsEnd(point: 't) : bool =
        this.Is(RangeRelation.MeetsEnd, Range.ofPoint point)
    
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.OverlapsStart(point: 't) : bool =
        this.Is(RangeRelation.OverlapsStart, Range.ofPoint point)
   
    /// Returns true if RangeRelation.OverlapsEnd holds
     member inline this.OverlapsEnd(point: 't) : bool =
        this.Is(RangeRelation.OverlapsEnd, Range.ofPoint point)
   
    /// Returns true if RangeRelation.Before holds
     member inline this.Contains(that: 't Range) : bool =
        Range.contains this that
    
    /// Returns true if the point does not lay outside this range
     member inline this.Contains(point: 't) : bool =
        Range.contains this (Range.ofPoint point)
    
    /// Returns true if this range does not exeed the bounds of the given range
     member inline this.Within(that: 't Range) : bool =
        Range.within this that


[<Extension>]
type Extensions =

    [<Extension>]
    /// Returns the Range between (a) and (a + offset)
     static member inline ToRangeOffset(a: ^t, offset: ^u) : ^t Range = 
        Range.ofSize offset a

    [<Extension>]    
    /// Returns a Range representing to bounds of the sequence
     static member ToRange (xs: #seq<'t>) : 't Range =  
        Range.ofSeq xs

    [<Extension>]    
    /// Create a Range of the single point
     static member ToRange (point: 't) : 't Range =  
        Range.ofPoint point
        
    [<Extension>]    
    /// Create a Range between the given points
     static member ToRange (a: 't, b: 't) : 't Range =  
        Range.ofBounds a b

    [<Extension>]    
    /// Create a Range between the given points
     static member ToRange (struct(a: 't, b: 't)) : 't Range =  
        Range.ofBounds a b

    [<Extension>]
    /// Sample a random point from the range
     static member Sample(rng: int Range) : int =
        Range.Sample.int rng

    [<Extension>]    
    /// Sample a random point from the range
    static member Sample(rng: float Range) : float =
        Range.Sample.float rng

    [<Extension>]    
    /// Sample a random point from the range
    static member Sample(rng: DateTime Range) : DateTime =
        Range.Sample.time rng

    [<Extension>]    
    /// Sample a random point from the range
    static member Sample(rng: int64 Range) : int64 =
        Range.Sample.long rng

    [<Extension>]
    /// Returns the size of the range
    static member inline Size(this) =
        Range.size this

    [<Extension>]
    /// Buffer the range by the given delta
    static member inline Buffer(this, delta:^u) : ^t Range =
        Range.buffer delta this
