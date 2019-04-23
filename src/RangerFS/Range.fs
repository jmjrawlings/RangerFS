namespace rec Ranger

open System
open FSharp.Core
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

#nowarn "0342"
#nowarn "86"

/// Allen's Interval Algebra relation table
/// https://en.wikipedia.org/wiki/Allen%27s_interval_algebra#Composition_of_relations_between_intervals
type Relation = 
    /// One or both the ranges are empty
    | Empty = 0

    /// <summary>
    /// X is entirely before Y
    /// <para>oxxooooo</para>
    /// <para>ooooxxxx</para>
    /// </summary>
    | Before = 1 

    /// <summary>
    /// X is entirely after Y
    /// <para>ooooxxxx</para>
    /// <para>oxxooooo</para>
    /// </summary>
    | After = 2

    /// <summary>
    /// X starts Y
    /// <para>oxxooooo</para>
    /// <para>oxxxxxxo</para>
    /// </summary>
    | Starts = 3 

    /// <summary>
    /// X is started by Y
    /// <para>oxxxxxxo</para>
    /// <para>oxxooooo</para>
    /// </summary>
    | StartedBy = 4
    
    /// <summary>
    /// X finishes Y
    /// <para>ooooooxx</para>
    /// <para>oooxxxxx</para>
    /// </summary>
    | Finishes = 5

    /// <summary>
    /// X is finished by Y
    /// <para>oooxxxxx</para>
    /// <para>ooooooxx</para>
    /// </summary>
    | FinishedBy = 6

    /// <summary>
    /// X ends where Y starts
    /// <para>oxxxooo</para>
    /// <para>oooxxxo</para>
    /// </summary>
    | MeetsStart = 7

    /// <summary>
    /// X starts where Y ends
    /// <para>oooxxxo</para>
    /// <para>oxxxooo</para>
    /// </summary>
    | MeetsEnd = 8

    /// <summary>
    /// End of X overlaps start of Y
    /// <para>xxxxxooo</para>
    /// <para>ooxxxxxx</para>
    /// </summary>
    | OverlapsStart = 9

    /// <summary>
    /// Start of X overlaps end of Y
    /// <para>ooxxxxxx</para>
    /// <para>xxxxxooo</para>
    /// </summary>
    | OverlapsEnd = 10

    /// <summary>
    /// X is entirely within Y
    /// <para>ooxxxooo</para>
    /// <para>xxxxxxxx</para>
    /// </summary>
    | Within = 11

    /// <summary>
    /// Y is entirely within X
    /// <para>xxxxxxxx</para>
    /// <para>ooxxxooo</para>
    /// </summary>
    | Contains = 12

    /// X = Y
    | Equal = 13

[<Struct>]
[<CustomComparison>]
[<StructuralEquality>]
/// Represents the interval between a Lower and Upper Bound
type Range<'t when 't : comparison> = 
    private
    | Empty_
    | Singleton_ of 't
    | Bounds_ of lo:'t * hi: 't    

    /// The lower bound of the Range
    member this.Lo : 't = 
        Range.lo this

    /// The upper bound of the Range
    member this.Hi : 't = 
        Range.hi this

    /// Returns true if the given range is the Empty Range
    member this.IsEmpty = 
        Range.isEmpty this

    /// Returns true if the Range contains a single value
    member this.IsSingleton = 
        Range.isSingleton this

    override this.ToString() =
        match this with 
        | Empty_          -> "{}"
        | Singleton_ p    -> sprintf "{%O}" p
        | Bounds_ (lo,hi) -> sprintf "{%O..%O}" lo hi

    interface IComparable with
        member this.CompareTo(that) =
            match that with
            | :? Range<'t> as r -> 
                Range.compare this r
            | _ ->
                failwith "Object was not a Range"

    interface IComparable<'t Range> with
        member this.CompareTo(that) = 
            Range.compare this that

    interface IRange<'t> with
        member this.Range = this

and IRange<'t when 't:comparison> = 
    abstract Range : 't Range

/// Thrown if certain operations are performed on the Empty Range
exception EmptyRangeException  

[<RequireQualifiedAccess>]
/// Functions to operate on Ranges
module Range = 
    
    [<Struct>]
    type private Comparison =
        | LT | GT | EQ 

    let inline private cmp a b : Comparison = 
        if      a < b then LT
        else if a > b then GT
        else    EQ

    /// Return the low value of the Range 
    let lo (r: 't Range) : 't =
        match r with
        | Bounds_ (x, _) 
        | Singleton_ x -> x
        | Empty_ -> raise EmptyRangeException

    /// Return the high value of the range
    let hi (r: 't Range) : 't =
        match r with
        | Bounds_ (_, x) 
        | Singleton_ x -> x
        | Empty_ -> raise EmptyRangeException

    /// Return the size of the Range
    let inline size (r: 't Range) : 'u =
        r.Hi - r.Lo

    /// Returns true if the given range is Empty
    let isEmpty (r: 't Range) : bool = 
        r = Empty_

    /// Returns true if range.Lo = range.Hi
    let isSingleton (r: 't Range) : bool = 
        match r with
        | Singleton_ _ -> true 
        | _ -> false

    /// The Empty Range
    let empty : 't Range = Empty_

    /// Construct a Range of a single value
    let singleton (p: 't) : 't Range =
        Singleton_ p

    /// Construct a non-empty Range if lo <= hi
    let ofBounds (lo: 't) (hi: 't) : 't Range =        
        match cmp lo hi with
        | LT -> Bounds_ (lo, hi)
        | EQ -> Singleton_ lo
        | GT -> Empty_

    /// Construct a Range that spans the given points
    let of2 (a: 't) (b: 't) : 't Range =
        match cmp a b with
        | LT -> Bounds_ (a, b)
        | EQ -> Singleton_ a
        | GT -> Bounds_ (b, a)

    /// Construct a Range from the given Tuple
    let ofTuple (a:'t, b:'t) : 't Range = 
        ofBounds a b

    /// Symmetric x = {-x..x}
    let inline symmetric (p: 't) : 't Range =
        of2 p -p

    /// Construct a Range with a single bound and a size
    let inline ofSize (delta: 'd) (point: 't)  : ^t Range = 
        of2 point (point + delta)

    /// Returns the union of two Ranges
    let union (a: 't Range) (b: 't Range) : 't Range =
        if      a.IsEmpty then b
        else if b.IsEmpty then a
        else 
            let lo = min a.Lo b.Lo
            let hi = max a.Hi b.Hi
            of2 lo hi

    /// Returns the union of many ranges
    let unionMany (ranges: 't Range seq) : 't Range = 
        Seq.fold union empty ranges

    let ofSeq(xs: #seq<'t>) : 't Range =
        xs 
        |> Seq.map singleton
        |> unionMany

    let toList (r: 't Range) : 't list = 
        match r with
        | Empty_ -> []
        | Singleton_ p -> [ p ]
        | Bounds_ (lo,hi) -> [lo; hi]

    let toSeq (r: 't Range) : 't seq =
        r
        |> toList
        |> Seq.ofList

    let toOption (r: 't Range): ('t * 't) option =
        if r.IsEmpty then None
        else Some (r.Lo, r.Hi)

    /// Returns the Zero range
    let inline zero () = 
        singleton LanguagePrimitives.GenericZero

    /// Returns the range from Zero to the given value
    let inline fromZero point =
        zero () |> of2 point

    /// Returns the Zero range
    let inline one () = 
        singleton LanguagePrimitives.GenericOne

    /// Returns the relation from a to b
    let relation (a: 't Range) (b: 't Range) : Relation =    
        match (a, b) with
        | Empty_, Empty_ -> Relation.Equal
        | Empty_, _ -> Relation.Empty
        | _, Empty_ -> Relation.Empty
        | _, _->
            match (cmp a.Lo b.Lo, cmp a.Hi b.Hi) with
            
            | EQ, EQ -> Relation.Equal
            | EQ, LT -> Relation.Starts
            | EQ, GT -> Relation.StartedBy
            | GT, EQ -> Relation.Finishes
            | LT, EQ -> Relation.FinishedBy
            | LT, GT -> Relation.Contains
            | GT, LT -> Relation.Within

            | LT, LT when a.Hi = b.Lo -> Relation.MeetsStart
            | LT, LT when a.Hi > b.Lo -> Relation.OverlapsStart
            | LT, LT -> Relation.Before

            | GT, GT when a.Lo = b.Hi -> Relation.MeetsEnd
            | GT, GT when a.Lo < b.Hi -> Relation.OverlapsEnd
            | GT, GT -> Relation.After 


    /// Returns the intersection of the given ranges
    let intersect (a: 't Range) (b: 't Range) : 't Range = 
        if a.IsEmpty || b.IsEmpty  then empty else 
    
        let lo = max a.Lo b.Lo
        let hi = min a.Hi b.Hi
        ofBounds lo  hi

    /// Returns true if b does not exceed the bounds of a
    let contains (a: 't Range) (b: 't Range) : bool =
        match relation a b with
        | Relation.StartedBy
        | Relation.FinishedBy
        | Relation.Contains
        | Relation.Equal -> true
        | Relation.Empty -> b.IsEmpty
        | _ -> true

    /// Returns true if a does not exceed the bounds of b
    let within (a: 't Range) (b: 't Range) : bool = 
        contains b a
        
    /// Returns true if the given Range intersect
    let intersects (a: 't Range) (b: 't Range) : bool =
        match relation a b with 
        | Relation.After 
        | Relation.Before -> false
        | _ -> true

    /// Returns true of the given Ranges do not intersect
    let disjoint (a: 't Range) (b: 't Range) : bool =
        not (intersects a b)

    /// Iterate over the Range with the given step 
    let inline iterate (step: ^step) (r: 't Range) : 't seq =
        if r.IsEmpty then Seq.empty else 
        let lo = r.Lo
        let hi = r.Hi
        let mutable x = r.Lo
        seq { while (x <= hi && x >= lo) do
              yield x
              x <- (x + step) }

    /// Partition the range into 'n' peices
    let inline partition (n: ^u) (r: ^t Range) : ^t Range seq =
        if r.IsEmpty then Seq.empty else

        let delta : ^t = 
            (Range.size r) / n
            
        r
        |> iterate delta
        |> Seq.pairwise
        |> Seq.map ofTuple

    /// Apply the given functions to the bounds of the Range
    let map (fLo: 't -> 'u) (fHi: 't -> 'u) (r: 't Range) : 'u Range =
        if r.IsEmpty
        then empty
        else of2 (fLo r.Lo) (fHi r.Hi)

    /// Apply the given function to the Lo value of the Range
    let mapLo (f: 't -> 't) (r: 't Range) : 't Range = 
         map f id r

    /// Apply the given function to the Hi value of the Range
    let mapHi (f: 't -> 't) (r: 't Range) : 't Range = 
        map id f r

    /// Apply the given function to the Range
    let map1 (f: 't -> 'u) (r: 't Range) : 'u Range = 
        map f f r

    /// Returns the union of cross-applying f to the given ranges
    let map2 (f: 't -> 't -> 'u) (a: 't Range) (b: 't Range) : 'u Range =
        a |> bind1 (fun a' -> 
            b |> map1 (fun b' ->
                f a' b'
                ))

    /// Apply the given function to the lower and upper bounds of the Range
    /// Any exception thrown return the Empty range
    let tryMap (fLo: 't -> 'u) (fHi: 't -> 'u) (r: 't Range) : 'u Range =
        try (map fLo fHi) r with | _ -> empty

    /// Apply the given functions to the lower and upper bounds of the Range
    let tryMap2 (f: 't -> 't -> 'u) (a: 't Range) (b: 't Range) : 'u Range =
        try (map2 f a b) with | _ -> empty

    let bind (fLo: 't -> 'u Range) (fHi: 't -> 'u Range) (r: 't Range) : 'u Range =
        if   r.IsEmpty 
        then empty
        else union (fLo r.Lo) (fHi r.Hi)

    let bind1 (f: 't -> 'u Range) (r: 't Range) : 'u Range = 
        bind f f r

    /// Bisect the first range returning the range before and after the second range
    let bisect (a: 't Range) (b: 't Range) : ('t Range * 't Range) =
        if a.IsEmpty || b.IsEmpty then (empty,empty) else 

        let before = 
            if b.Lo > a.Hi then a else ofBounds a.Lo b.Lo

        let after = 
            if b.Hi < a.Lo then a else ofBounds b.Hi a.Hi

        (before, after)

    /// Buffer the range by the given delta
    let inline buffer (delta: ^u) (r: ^t Range) : ^t Range =
        map
            (fun p -> p - delta)
            (fun p -> p + delta)
            r

    /// Clamp the given point such to the range
    let clampPoint (point: 't) (a: 't Range) : 't =
        match a with
        | _ when a.IsEmpty -> raise EmptyRangeException
        | Singleton_ x -> x
        | Bounds_ (lo, _) when point < lo -> lo
        | Bounds_ (_, hi) when point > hi -> hi
        | _ -> point

    /// <summary>
    /// Clamp the second range such that it does not exceed the first
    /// </summary>
    let clamp (a: 't Range) (b: 't Range) : 't Range =
        if a.IsEmpty then empty else
        if b.IsEmpty then singleton a.Lo else
        let lo = clampPoint b.Lo a
        let hi = clampPoint b.Hi a
        ofBounds lo hi

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

    /// Ranges are ordered by LowerBound first then UpperBound
    let internal compare this that : int = 
        if      this.IsEmpty  then -1
        else if that.IsEmpty  then 1
        else 
            let lo = cmp this.Lo that.Lo
            let hi = cmp this.Hi that.Hi

            match (lo, hi) with
            | LT, _  
            | EQ, LT -> -1
            | EQ, EQ -> 0
            | _ ->      1

    /// Returns the Haursoff Distance between the given intervals
    /// https://en.wikipedia.org/wiki/Hausdorff_distance
    let inline haursoff (a: ^t Range) (b: ^t Range) : ^u = 
        let sub a b = map2 (-) a b       
        let delta = 
            if a > b then sub a b else sub b a
        hi delta


/// Operator support and Member Access
type Range<'t when 't:comparison> with

    static member inline (~-) (a) =
       Range.map1 (~-) a

    static member inline (~+) (a) =
       Range.map1 (~+) a

    static member inline (+) (a, b) =
       Range.map2 (+) a b

    static member inline (+) (a, b) =
       Range.map2 (+) a (Range.singleton b)

    static member inline (-) (a, b) =
       Range.map2 (-) a b

    static member inline (-) (a, b) =
       Range.map2 (-) a (Range.singleton b)

    static member inline (*) (a, b) =
       Range.map2 (*) a b

    static member inline (*) (a, b) =
       Range.map2 (*) a (Range.singleton b)

    static member inline (/) (a, b) =
       Range.map2 (/) a b

    static member inline (/) (a, b) =
       Range.map2 (/) a (Range.singleton b)

    static member inline (%) (a, b) =
       Range.map2 (%) a b

    static member inline (%) (a, b) =
       Range.map2 (%) a (Range.singleton b)

    static member inline Abs r =
        Range.map1 abs r
    
    /// Map the given function to both bounds of the range
    member this.Map f =
       Range.map1 f this

    /// Map the given function to the Lower Bound of the range
    member this.MapLo f =
       Range.mapLo f this

    /// Map the given function to the Lower Bound of the range
    member this.MapHi f =
       Range.mapHi f this

    /// Map the given functions to the bounds of the range
    member this.Map(fLo,fHi) =
       Range.map fLo fHi this
    
    /// Bind the given functions to the bounds of the range
    member this.Bind(fLo,fHi) = 
       Range.bind fLo fHi this
    
    /// Bind the given functions to the bounds of the range
    member this.Bind f =
       Range.bind1 f this
    
    /// Returns the union of the range with the given point
    member this.Union point =
       Range.union this (Range.singleton point)
    
    /// Returns the union of the two ranges
    member this.Union range = 
       Range.union this range
   
    /// Bisect the Range at the given point
    member this.Bisect point =
       Range.bisect this (Range.singleton point)    

    /// Bisect the Range at the given point
    member this.Bisect range =
       Range.bisect this range

    /// Returns the Relation from a -> b, or from b -> a if inverse is true
    member this.Relation(range: 't Range, [<Optional;DefaultParameterValue(false)>]inverse:bool) : Relation = 
        if inverse then
            Range.relation range this
        else
            Range.relation this range

    /// Returns the Relation from a -> b, or from b -> a if inverse is true
    member this.Relation(point: 't, [<Optional;DefaultParameterValue(false)>]inverse:bool) : Relation = 
        this.Relation(Range.singleton point, inverse)
    
    /// Check if the relationship holds between the two ranges
    member this.HasRelation(relation:Relation, range: 't Range, [<Optional;DefaultParameterValue(false)>]inverse:bool) : bool =
       this.Relation(range, inverse) = relation
    
    /// Check if the relationship holds between the two ranges
    member this.HasRelation(relation:Relation, point: 't, [<Optional;DefaultParameterValue(false)>]inverse:bool) : bool =
       this.HasRelation(relation, Range.singleton point)
    
    /// Returns true if the given range does not exceed the bound of this range
    member this.Contains range =
       Range.contains this range
    
    /// Returns true if the point does not exceed the bounds of this range
    member this.Contains point =
       Range.contains this (Range.singleton point)
    
    /// Returns true if this range does not exeed the bounds of the given range
    member this.Within that =
       Range.within this that

    /// Clamp the given range to this one
    member this.Clamp that =
        Range.clamp this that

    /// Clamp the give point to this range
    member this.Clamp that =
        Range.clampPoint that this


/// Operators for easy construction
module Operators = 
    let (<=>) a b = Range.ofBounds a b
    let (<~>) a b = Range.of2 a b
    let (!) p     = Range.singleton p
    let inline (=+>) a b  = Range.ofSize b a
       

[<Extension>]
/// Extension methods for ease of use
type Extensions =

    [<Extension>]
    /// Returns the Range of the given size
     static member inline ToRangeOfSize(a: ^t, size: ^u) : ^t Range = 
        Range.ofSize size a

    [<Extension>]    
    /// Returns a Range representing to bounds of the sequence
     static member ToRange (xs: #seq<'t>) : 't Range =  
        Range.ofSeq xs

    [<Extension>]    
    /// Create a Range of the single point
     static member ToRange (point: 't) : 't Range =  
        Range.singleton point
        
    [<Extension>]    
    /// Create a Range between the given points
     static member ToRange (a: 't, b: 't, [<Optional;DefaultParameterValue(false)>] forceOrder:bool) : 't Range =  
        if forceOrder 
        then Range.ofBounds a b
        else Range.of2 a b

    [<Extension>]    
    /// Create a Range between the given points
     static member ToRange (struct(a: 't, b: 't)) : 't Range =  
        Range.of2 a b

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
    static member inline Size(this) : ^u =
        Range.size this

    [<Extension>]
    /// Buffer the range by the given delta
    static member inline Buffer(this, delta:^u) : ^t Range =
        Range.buffer delta this

    [<Extension>]
    /// Buffer the range by the given delta
    static member inline Iterate(this, step:^u) : ^u seq =
        Range.iterate step this

    [<Extension>]
    /// Alias for Size
    static member Duration(this : DateTime Range) : TimeSpan =
        this.Size()

    
