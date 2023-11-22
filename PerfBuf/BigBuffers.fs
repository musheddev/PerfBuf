namespace PerfBuf



open System.Threading
open System.Runtime.InteropServices
open System
open AffineThreadPool
open System.Runtime.CompilerServices
open FSharp.Control
open FSharp.Control.Tasks
open System.Threading.Tasks



let inline lengthTo(c1 : int,c2 : int,bufferSize : int) = if c1 < c2 then c2 - c1 else bufferSize - c1 + c2

let inline minLength(c1 : int,((c2,len) : int[]*int),bufferSize) =
    let mutable min = bufferSize
    for i=0 to (len-1) do
        let l = lengthTo(c1,c2.[i],bufferSize)
        if l < min then min <- l
    min

[<Struct>]
type Window = 
    | NoSpace
    | One of (int*int)
    | Two of a:(int*int) * b:(int*int)
    
type Cursor =
    //inherit Default1
    //static member inline Get (x: byref<int>    , [<Optional>]_impl: Cursor) = Volatile.Read(&x)
    //static member inline Get (x: 'a []         , [<Optional>]_impl: Cursor ) = x
    //static member inline Get (x: 'a ResizeArray, [<Optional>]_impl: Cursor ) = Seq.toArray x
    

    //static member inline Get value : int = 
    //    let inline call_2 (a: ^a, b: ^b) = ((^a or ^b) : (static member Cursor : _*_ -> _) b, a)
    //    let inline call (a: 'a, b: 'b) = call_2 (a, b)
    //    call (Unchecked.defaultof<Cursor>, value)

     
    static member inline MinLength (c1: byref<int>, c2: byref<int>, bufferSize : int, [<Optional>]_impl: Cursor) = 
        lengthTo(Volatile.Read(&c1),Volatile.Read(&c2),bufferSize)
        
    static member inline MinLength (c1: byref<int>, c2: int[]*int, bufferSize : int, [<Optional>]_impl: Cursor) = 
        minLength(Volatile.Read(&c1),c2,bufferSize)
        
    static member inline IMinLength c1 c2 bufferSize : int = 
        let inline call_2 (a: ^a, b: ^b, c: ^c, d) = ((^a or ^b or ^c) : (static member MinLength : _*_*_*_ -> _) c1, c2, bufferSize, a)
        let inline call (a: 'a, b: 'b, c: 'c ,d) = call_2 (a, b, c, d)
        call (Unchecked.defaultof<Cursor>, c1, c2, bufferSize)
   
   





open FSharp.NativeInterop

let inline stackalloc<'a when 'a: unmanaged> (length: int): Span<'a> =
  let p = NativePtr.stackalloc<'a> length |> NativePtr.toVoidPtr
  Span<'a>(p, length)
        
//let len = Vector<uint32>.Count //outputs 8        

//[<Struct>]
//type Window = { x:int; y:int }
    


    
//[<Struct>]    
//type Cur =
//    { mutable x : int }
//    member this.Get() = Volatile.Read(&this.x)
//    member this.Set(v : byref<int>, value) = Volatile.Write(&v,value)
//    member this.Length(otherCur : unit -> int) =
//        let y = otherCur()
//        let x = this.Get()
//        if x < y then y - x else bufferSize - readCur + writeCur
[<Struct; IsByRefLike>]
type Fixed<'t> = 
    { 
        mutable ary : 't[]; 
        mutable len : int 
    }
    with static member Create(maxLen) =
        { ary = Array.zeroCreate maxLen; len = 0 }

type Cur =
    {
        mutable x : int
        mutable vts : ValueTaskSource
        mutable len : int 
    }

//type ICur =
//    abstract member TryNext : unit -> Window2
//    abstract member Advance : int -> bool
//    abstract member Availble : unit -> int

//type Cur<'T>(c1 : byref<int>,c2,bufferSize,maxWindow : int) =
//    interface ICur<'T> with
//        member this.TryNext() = 
//            let minLength = min (Cursor.MinLength(&c1,c2,bufferSize)) maxWindow
//            if minLength = 0 then Window2.NoSpace
//            else 
//                let offset = Cursor.Get &c1 
//                let overflow = minLength + offset - bufferSize
//                if overflow > 0 then
//                    Window2.Two({x = offset; y = bufferSize - offset}, {x = 0; y = overflow})
//                else 
//                    Window2.One({x = offset; y = minLength})
            
        

//let inline lengthTo(c1,c2,bufferSize) = if c1 < c2 then c2 - c1 else bufferSize - c1 + c2

//let inline minLength(c1,c2 : int[],bufferSize) =
//    let mutable min = bufferSize
//    for c2 in c2 do
//        let l = lengthTo(c1,c2,bufferSize)
//        if l < min then min <- l
//    min
    
//type Indexer1On1 (bufferSize: int, maxWindowSize: int) =
//    do if bufferSize <= 0 then invalidArg "bufferSize" "The bufferSize must be greater than 0."

//    let mutable readCur = 0
//    let mutable writeCur = 0

//    member this.GetWriteCur() = 
//        let inline minLen() = Cursor.MinLength(&writeCur, &readCur,bufferSize)
//        let inline advance(len) = Interlocked.Add(&writeCur, len)
//        let inline read() = Volatile.Read(&writeCur)
        
//        { new ICur with
//            member this.Availble(): int = minLen()

//            member this.Advance(len : int): bool = 
//                if 0 < len && len < minLen() then 
//                    advance(len) |> ignore
//                    true
//                else false
            
//            member this.TryNext() = 
//                let minLength = min (minLen()) maxWindowSize
//                if minLength = 0 then Window2.NoSpace
//                else 
//                    let offset = read()
//                    let overflow = minLength + offset - bufferSize
//                    if overflow > 0 then
//                        Window2.Two({x = offset; y = bufferSize - offset}, {x = 0; y = overflow})
//                    else 
//                        Window2.One({x = offset; y = minLength})}

//    member this.GetReadCur() = 
//        let inline minLen() = Cursor.MinLength(&readCur, &writeCur,bufferSize)
//        let inline advance(len) = Interlocked.Add(&readCur, len)
//        let inline read() = Volatile.Read(&readCur)
        
//        { new ICur with
//            member this.Availble(): int = minLen()

//            member this.Advance(len : int): bool = 
//                if 0 < len && len < minLen() then 
//                    advance(len) |> ignore
//                    true
//                else false
            
//            member this.TryNext() = 
//                let minLength = min (minLen()) maxWindowSize
//                if minLength = 0 then Window2.NoSpace
//                else 
//                    let offset = read()
//                    let overflow = minLength + offset - bufferSize
//                    if overflow > 0 then
//                        Window2.Two({x = offset; y = bufferSize - offset}, {x = 0; y = overflow})
//                    else 
//                        Window2.One({x = offset; y = minLength})}


//    type Indexer1OnM (bufferSize: int, maxWindowSize: int, maxReaders) =
//        do if bufferSize <= 0 then invalidArg "bufferSize" "The bufferSize must be greater than 0."
    
//        let mutable readCur = Array.zeroCreate<int> maxReaders
//        let mutable writeCur = 0
//        let mutable readerCount = 0

//        member this.GetWriteCur() = 
//            let inline minLen() = Cursor.MinLength(&writeCur,(readCur,readerCount),bufferSize)
//            let inline advance(len) = Interlocked.Add(&writeCur, len)
//            let inline read() = Volatile.Read(&writeCur)
        
//            { new ICur with
//                member this.Availble(): int = minLen()

//                member this.Advance(len : int): bool = 
//                    if 0 < len && len < minLen() then 
//                        advance(len) |> ignore
//                        true
//                    else false
                
//                member this.TryNext() = 
//                    let minLength = min (minLen()) maxWindowSize
//                    if minLength = 0 then Window2.NoSpace
//                    else 
//                        let offset = read()
//                        let overflow = minLength + offset - bufferSize
//                        if overflow > 0 then
//                            Window2.Two({x = offset; y = bufferSize - offset}, {x = 0; y = overflow})
//                        else 
//                            Window2.One({x = offset; y = minLength})}

//        member this.GetReadCur() = 
//            //use locks to prevent race??
//            let index = Interlocked.Increment(&readerCount) - 1
//            let inline minLen() = Cursor.MinLength((&readCur.[index]),&writeCur,bufferSize)
//            let inline advance(len) = Interlocked.Add(&readCur.[index], len)
//            let inline read() = Volatile.Read(&readCur.[index])
            
//            { new ICur with
//                member this.Availble(): int = minLen()

//                member this.Advance(len : int): bool = 
//                    if 0 < len && len < minLen() then 
//                        advance(len) |> ignore
//                        true
//                    else false
                
//                member this.TryNext() = 
//                    let minLength = min (minLen()) maxWindowSize
//                    if minLength = 0 then Window2.NoSpace
//                    else 
//                        let offset = read()
//                        let overflow = minLength + offset - bufferSize
//                        if overflow > 0 then
//                            Window2.Two({x = offset; y = bufferSize - offset}, {x = 0; y = overflow})
//                        else 
//                            Window2.One({x = offset; y = minLength})}
                            

//    type SpanCursor<'T when 'T : not struct>(raw : ICur,buffer : 'T[]) =
//        let mutable rented = 0
//        member this.Availble() = raw.Availble() 
//        member this.TryNext() =
//            if rented <> 0 then invalidOp "Span already rented."
//            match raw.TryNext() with
//            | Window2.NoSpace -> Span.Empty
//            | Window2.One(w) -> 
//                let span = buffer.AsSpan(w.x,(w.y - w.x))
//                rented <- span.Length
//                span
//            | Window2.Two(w1,w2) -> 
//                let span1 = buffer.AsSpan(w1.x,(w1.y - w1.x))
//                //let span2 = buffer.AsSpan(w2.x,(w2.y - w2.x))
//                rented <- span1.Length //+ span2.Length
//                span1
//        member this.Return() =
//            if rented > 0 then 
//                raw.Advance(rented) |> ignore
//                rented <- 0
//            else invalidOp "Span not rented."



//    type SpanCursor2<'T when 'T : not struct>(raw : ICur,buffer : 'T[],queue,notify) =
//        let mutable rented = 0
//        let mutable vts = ValueTaskSource()
//        member this.Availble() = raw.Availble() 
//        member this.TryNext() =
//            if rented <> 0 then invalidOp "Span already rented."
//            match raw.TryNext() with
//            | Window2.NoSpace -> Span.Empty //queue waiter return valuetask from vts
//            | Window2.One(w) -> 
//                let span = buffer.AsSpan(w.x,(w.y - w.x))
//                rented <- span.Length
//                span
//            | Window2.Two(w1,w2) -> 
//                let span1 = buffer.AsSpan(w1.x,(w1.y - w1.x))
//                //let span2 = buffer.AsSpan(w2.x,(w2.y - w2.x))
//                rented <- span1.Length //+ span2.Length
//                span1
//        member this.Return() =
//            if rented > 0 then 
//                raw.Advance(rented) |> ignore
//                rented <- 0
//            else invalidOp "Span not rented."
            

            
    type CBuff<'t when 't : not struct>(bufferSize: int, maxWindowSize: int, maxReaders: int, maxWriters : int) =
        let buf = Array.zeroCreate<'t> bufferSize

        let mutable writeCurs = Array.zeroCreate<int> maxWriters
        let mutable readCurs = Array.zeroCreate<int> maxReaders
        let mutable readerCount = 0
        let mutable writerCount = 0
        
        let mutable writeRented = Array.zeroCreate<int> maxWriters
        let mutable readRented = Array.zeroCreate<int> maxReaders
        
        let mutable writeWaiters = Array.init<ValueTaskSource> maxWriters (fun _ -> ValueTaskSource())
        let mutable readWaiters = Array.init<ValueTaskSource> maxReaders (fun _ -> ValueTaskSource())
        let mutable writeWaitCount = 0
        let mutable readWaitCount = 0

        let lockObj = new obj()
    
        let mkReader =
            let inline r_minLen(index) = Cursor.MinLength((&readCurs.[index]),(writeCurs,writerCount),bufferSize)
            let inline r_advance(index,len) = Interlocked.Add(&readCurs.[index], len)
            let inline r_read(index) = Volatile.Read(&readCurs.[index])
            
            let inline r_advance2(index) = 
                let len = readRented.[index]
                if 0 < len && len < r_minLen(index) then 
                    r_advance(index,len) |> ignore
                    true
                else false

            let inline r_advance3(index) =
                let waitCur = readCurs.[index]
                if r_advance2(index) then
                    for i = 0 to readCurs.Length-1 do
                        if writeCurs.[i] = waitCur then
                            if not <| writeWaiters.[i].TrySetResult() then failwith "not waiting"


                
            let inline r_next(index) =
                let minLength = min (r_minLen(index)) maxWindowSize
                if minLength = 0 then 
                    Window.NoSpace
                else 
                    let offset = r_read(index)
                    let overflow = minLength + offset - bufferSize
                    if overflow > 0 then
                        Window.Two((offset,bufferSize - offset),(0,overflow))
                    else 
                        Window.One((offset,minLength))

            let inline r_await(index) =
                let v = readWaiters.[index].TryInitialize()
                
                vtask {
                    do! v
                    return r_next(index)
                }

            let inline r_next2(index) =
                match r_next(index) with
                | Window.NoSpace -> r_await(index)
                | x -> ValueTask.FromResult(x)
                
            let inline apply (index) (consume : 't array * int * int -> unit) =
                vtask { 
                    try 
                        let! win = r_next2(index)
                        match win with
                        | Window.One(c,len) -> do consume(buf,c,len)
                        | Window.Two((c1,len1),(c2,len2)) ->
                            do consume(buf,c1,len1)
                            do consume(buf,c2,len2)
                        | Window.NoSpace -> failwith "shouldn't happen"
                    finally 
                        r_advance3(index)
                    } 


            fun () ->
                let index = (lock lockObj) (fun _ ->
                    if readerCount > maxReaders then failwith "reader count exhausted"
                    let index = Interlocked.Increment(&readerCount) - 1
                    index)
                apply index
                    

        let mkWriter =
            let inline w_minLen(index) = Cursor.MinLength(&writeCurs.[index],(readCurs,readerCount),bufferSize)
            let inline w_advance(index,len) = Interlocked.Add(&writeCurs.[index], len)
            let inline w_read(index) = Volatile.Read(&writeCurs.[index])
            let inline w_advance2(index) = 
                let len = writeRented.[index]
                if 0 < len && len < w_minLen(index) then 
                    w_advance(index,len) |> ignore
                    true
                else false

            let inline w_advance3(index) =
                let waitCur = writeCurs.[index]
                if w_advance2(index) then
                    for i = 0 to readCurs.Length-1 do
                        if readCurs.[i] = waitCur then
                            if not <| readWaiters.[i].TrySetResult() then failwith "not waiting"


                
            let inline w_next(index,req) =
                let minLength = min (w_minLen(index)) (min maxWindowSize req)
                if minLength = 0 then 
                    Window.NoSpace
                else 
                    let offset = w_read(index)
                    let overflow = minLength + offset - bufferSize
                    if overflow > 0 then
                        Window.Two((offset,bufferSize - offset),(0,overflow))
                    else 
                        Window.One((offset,minLength))

            let inline w_await(index,req) =
                let v = writeWaiters.[index].TryInitialize()
                
                vtask {
                    do! v
                    return w_next(index,req)
                }

            let inline w_next2(index,req) =
                match w_next(index,req) with
                | Window.NoSpace -> w_await(index,req)
                | x -> ValueTask.FromResult(x)
                
            let inline apply (index) (req) (consume : 't array * int * int -> unit) =
                vtask { 
                    try 
                        let! win = w_next2(index,req)
                        match win with
                        | Window.One(c,len) -> do consume(buf,c,len)
                        | Window.Two((c1,len1),(c2,len2)) ->
                            do consume(buf,c1,len1)
                            do consume(buf,c2,len2)
                        | Window.NoSpace -> failwith "shouldn't happen"
                    finally 
                        w_advance3(index)
                    } 

            fun () ->
                let index = (lock lockObj) (fun _ ->
                    if writerCount > maxWriters then failwith "exhausted writer count"
                    let index = Interlocked.Increment(&writerCount) - 1
                    index)
                apply index

        member this.MakeWriter() = mkWriter()

        member this.MakeReader() = mkReader()
        
                    
[<RequireQualifiedAccess>]
module ComputeBuffers =
    open BigBuffers

    let mkSink (src : taskSeq<'t[]>) (buff : CBuff<'t>) affinityId =
        let writer = buff.MakeWriter()
        let t = 
            TaskSeq.iterAsync (fun (x : 't[]) -> 
                let xlen = x.Length
                let mutable left = xlen
                let mutable i = 0
                let apply (ary,p,len) =
                    if len <= xlen then 
                       x.AsSpan(i,len).CopyTo(ary.AsSpan(p,len))
                       left <- left - len
                       i<- i + len
                    else failwith "unxpected"   
                let v = vtask {
                    while left > 0 do
                        do! writer left apply 

                }
                v.AsTask()
                ) src
        task { 
            SynchronizationContext.SetSynchronizationContext(AffineSynchronizationContex(affinityId)) 
            do! t }


    let mkSource (buff : CBuff<'t>) maxSpan affinityId =
        let reader = buff.MakeReader()
        taskSeq {
            while true do 
                let apply (ary,p,len) =

        
        }