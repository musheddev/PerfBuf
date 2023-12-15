module PerfBuf.ThreadedActor


open System
open System.Collections.Concurrent
open System.Threading
open FSharp.Control
open FSharp.Control.Tasks
open System.Threading.Tasks
open AffineThreadPool
open Microsoft.FSharp.Control.ValueTaskBuilder
open Microsoft.FSharp.Control.ValueTaskBuilderExtensions.LowPriority
open Microsoft.FSharp.Control.ValueTaskBuilderExtensions.MediumPriority
open Microsoft.FSharp.Control.ValueTaskBuilderExtensions.HighPriority

module ActorStatus =

    [<Literal>]
    let Idle = 0L

    [<Literal>]
    let Occupied = 1L

    [<Literal>]
    let Stopped = 2L

/// <summary> An actor pulls a message off of a (thread-safe) queue and executes it in the isolated context of the actor. </summary>
/// <typeparam name = "tmsg"> the type of messages that this actor will manage </typeparam>
/// <param name = "bodyFn"> the function to execute when the thread is available </param>
type ThreadedActor<'tmsg>(bodyFn: 'tmsg * ThreadedActor<'tmsg> -> unit) as this =

    let mutable status: int64 = ActorStatus.Idle

    //Do not expose these
    let signal = new ManualResetEventSlim(false)
    let queue = ConcurrentQueue<'tmsg>()
    let mutable count = 0
    // This is the main thread function (delegate). It gets executed when the thread is started/signaled
    let threadFn () =
        // While the actor is not stopped, check the queue and process any awaiting messages
        while Interlocked.Read(&status) <> ActorStatus.Stopped do
            while not queue.IsEmpty do
                //assert(queue.Count = Threading.Volatile.Read(&count)) race condition
                // If the thread is idle, update it to 'occupied'
                Interlocked.CompareExchange(&status, ActorStatus.Occupied, ActorStatus.Idle)
                |> ignore
                // Try to get the next message in the queue
                let isSuccessful, message = queue.TryDequeue()
                // If we successfully retrieved the next message, execute it in the context of this thread
                if isSuccessful then 
                    bodyFn (message, this)
                    if Threading.Interlocked.Decrement(&count) = 0 then 
                        signal.Reset()
            // If the thread is 'occupied', mark it as idle
            Interlocked.CompareExchange(&status, ActorStatus.Idle, ActorStatus.Occupied) |> ignore
            signal.Wait()
        // If the thread is stopped, dispose of it
        signal.Dispose()

    // The thread associated with this actor (one-to-one relationship)
    // Pass the threadFn delegate to the constructor
    let thread =
        Thread(ThreadStart(threadFn), IsBackground = true, Name = "ActorThread")

    // Start the thread
    do thread.Start()

    /// Enqueue a new messages for the thread to pick up and execute
    member this.Enqueue(msg: 'tmsg) =
        if Interlocked.Read(&status) <> ActorStatus.Stopped then
            queue.Enqueue(msg)
            if Threading.Interlocked.Increment(&count) = 1 then 
                signal.Set()
        else
            failwith "Cannot queue to stopped actor."

    // Get the length of the actor's message queue
    member this.QueueCount = queue.Count

    // Stops the actor
    member this.Stop() =
        Interlocked.Exchange(&status, ActorStatus.Stopped)
        |> ignore

        signal.Set()

    interface IDisposable with
        member __.Dispose() =
            this.Stop()
            thread.Join()

            
type WaitHandle with
    
    member this.WaitOneAsync(timeout : int) =
        let tcs = new TaskCompletionSource<bool>()
        let rwh = ThreadPool.RegisterWaitForSingleObject(this, WaitOrTimerCallback(fun _ timedOut -> tcs.TrySetResult(not timedOut) |> ignore), null, timeout, true)
        task {
            let! r = tcs.Task
            rwh.Unregister(null) |> ignore
            return r
        }

        
type ConcurrentQueue<'tmsg> with

    member this.CreateTaskSeq (token : CancellationToken) =
        taskSeq {
            while not token.IsCancellationRequested do
                while not this.IsEmpty do
                    let isSuccessful, message = this.TryDequeue()
                    if isSuccessful then 
                        yield message
                if this.IsEmpty then 
                    do! Task.Delay(50)
        }
    
    member this.CreateThreadedProc (name) (bodyFn : 'tmsg -> unit) (token : CancellationToken) =
        let threadFn () =
            while not token.IsCancellationRequested do
                while not this.IsEmpty do
                    let isSuccessful, message = this.TryDequeue()
                    if isSuccessful then 
                        bodyFn (message)
                if this.IsEmpty then 
                    Thread.Sleep(50)
        let thread =
            Thread(ThreadStart(threadFn), IsBackground = true, Name = name)

        do thread.Start()
        thread
            
type AsyncActor<'tmsg>(queue : ConcurrentQueue<'tmsg>) as this =

    let mutable status: int64 = ActorStatus.Stopped
    let vts = ValueTaskSource()

    let vts2 = ValueTaskSource()
    //Do not expose these
    //let signal = new ManualResetEventSlim(false)
    let mutable count = queue.Count |> uint64

    member this.Run () =
        if Interlocked.CompareExchange(&status, ActorStatus.Occupied, ActorStatus.Stopped) = ActorStatus.Stopped then
            taskSeq {
                while Interlocked.Read(&status) <> ActorStatus.Stopped do
                    while not queue.IsEmpty do
                        //assert(queue.Count = Threading.Volatile.Read(&count)) race condition
                        
                        let isSuccessful, message = queue.TryDequeue()
                        if isSuccessful then 
                            yield message
                            //if Threading.Interlocked.Decrement(&count) >= 100UL then
                            //    vts.TrySetResult()
                            if (Threading.Interlocked.Decrement(&count) = 0UL) then
                                () //do! Task.Delay(50)
                    if queue.IsEmpty then 
                        do! Task.Delay(50)
                }
        else 
            failwith "Cannot be run concurrently"

    /// Enqueue a new messages for the thread to pick up and execute
    member this.Enqueue(msg: 'tmsg) =
            queue.Enqueue(msg)
            let cnt =Threading.Interlocked.Increment(&count)
            cnt
            
    // Get the length of the actor's message queue
    member this.QueueCount() = Interlocked.Read(&count)

    // Stops the actor
    member this.Stop() =
        Interlocked.Exchange(&status, ActorStatus.Stopped)
        |> ignore


    interface IDisposable with
        member __.Dispose() =
            this.Stop()
 
 type ThreadedActor2<'tmsg>(bodyFn: 'tmsg -> unit, maxLen,  queue : ConcurrentQueue<'tmsg>, ct : CancellationToken ) as this =
 
 
     //Do not expose these
     let signal = new ManualResetEventSlim(false) //avoids spin
     let rsignal = new ManualResetEventSlim(false) //throttles
     
 
 
     let threadFn () =
         while not <| ct.IsCancellationRequested do
             while not queue.IsEmpty do
                 let isSuccessful, message = queue.TryDequeue()
                 // If we successfully retrieved the next message, execute it in the context of this thread
                 if isSuccessful then 
                     bodyFn message
                 if queue.Count <= maxLen && (not rsignal.IsSet) then rsignal.Set()
 
             if queue.IsEmpty then 
                 signal.Reset()
                 signal.Wait()
         signal.Dispose()
 
 
     let thread =
         Thread(ThreadStart(threadFn), IsBackground = true, Name = "ActorThread")
 
     do thread.Start()
 
 
     member this.Enqueue(msg: 'tmsg) =
         if queue.Count < maxLen then
             queue.Enqueue(msg)
             if not <| signal.IsSet then signal.Set()
 
         else
             rsignal.Reset()
             rsignal.Wait() //blocking throttle
             queue.Enqueue(msg)
 
 
     member this.QueueCount = queue.Count
 
     interface IDisposable with
         member __.Dispose() =
             thread.Join()
 
type VACore4<'tmsg> = ConcurrentQueue<'tmsg>
 
module AsyncChannels4 =
    
    let init (qDepth : uint64) ct : VACore4<'tmsg> =
        let queue = new ConcurrentQueue<'tmsg>()
        queue
        
    let enqueue (core : VACore4<'tmsg>) (msg : 'tmsg) =
        core.Enqueue(msg)
 
type VACore3<'tmsg> = 
    {
        cts : CancellationTokenSource
        signal: ManualResetEventSlim
        rsignal: ManualResetEventSlim
        queue: ConcurrentQueue<'tmsg>
        mutable thread : Thread
        maxLen : uint64
        mutable bodyFn : ('tmsg -> unit)[]
    }

module AsyncChannels3 =

    let init<'t> (qDepth : uint64) ct : VACore3<'t> =
        let queue = new ConcurrentQueue<'t>()
        let signal = new ManualResetEventSlim(false) //avoids spin
        let rsignal = new ManualResetEventSlim(false) //throttles
        let cts = new CancellationTokenSource()

        let core = {cts = cts; signal = signal; rsignal = rsignal; queue = queue; thread = Unchecked.defaultof<Thread>; maxLen = qDepth; bodyFn = [||]}
        let threadFn () =
            while not <| cts.Token.IsCancellationRequested do
                while not queue.IsEmpty do
                    let isSuccessful, message = queue.TryDequeue()
                    // If we successfully retrieved the next message, execute it in the context of this thread
                    if isSuccessful then 
                        for fn in core.bodyFn do
                            fn message
                    if uint64 queue.Count <= qDepth && (not rsignal.IsSet) then rsignal.Set()

                if queue.IsEmpty then 
                    signal.Reset()
                    signal.Wait()
        let thread =
            Thread(ThreadStart(threadFn), IsBackground = true, Name = "AsyncChannels3<" + nameof<'t> + ">")

        do thread.Start()
        core.thread <- thread
        core
        

    let enqueue<'t> (core : VACore3<'t>) (msg : 't) =
        if uint64 core.queue.Count < core.maxLen then
            core.queue.Enqueue(msg)
            if not <| core.signal.IsSet then core.signal.Set()

        else
            core.rsignal.Reset()
            core.rsignal.Wait() //blocking throttle
            core.queue.Enqueue(msg)
            

    let iter (core : VACore3<'t>)  (fn : 't -> unit) =
        core.bodyFn <- Array.insertAt 0 fn core.bodyFn
        
    let run core =
        let channel = System.Threading.Channels.Channel.CreateUnbounded()
        let fn v = 
            channel.Writer.TryWrite(v) |> ignore
        iter core fn
        channel.Reader.ReadAllAsync(core.cts.Token)

    let debounce core count (msec : int) =
        let buffer = Array.zeroCreate<'t> count
        let mutable cnt = 0
        let mutable clock = Unchecked.defaultof<Timer>
        let core2 = init<'t[]> 100UL core.cts
        let reset _ =
            let count = Interlocked.Exchange(&cnt, 0)
            if count > 0 then
                enqueue core2 (Array.take count buffer)  //can overwhelm the actor
            clock.Change(msec,msec*100) |> ignore
            
            
        do clock <- new Timer(TimerCallback(reset))
        
        let fn v =
            let count = Interlocked.Increment(&cnt)
            buffer.[count-1] <- v
            if count = buffer.Length then
                reset ()
        iter core fn  

        core2
  
open System.Threading.Channels            
type VACore2<'tmsg> =
    {
        mutable count: uint64
        channel: Channel<'tmsg>
        ct: CancellationToken
    }

[<RequireQualifiedAccess>]
module AsyncChannels2 =
    
    let init<'t> (qDepth : uint64) ct : VACore2<'t> =
        let channel = Channel.CreateBounded(int qDepth)
        let mutable count = 0UL
        {
            count = count
            channel = channel
            ct = ct
        }

    let enqueue core item =
        core.channel.Writer.WriteAsync(item, core.ct) 

    let run core =
        core.channel.Reader.ReadAllAsync(core.ct)
       
    let mult core =
        let bag = ResizeArray<VACore2<'t>>()
        TaskSeq.iterAsync (fun v ->
            task {
                for x in bag do
                    do! enqueue x v
            }
        ) (run core) |> ignore
        fun v -> bag.Add(v)
            
    let map fn core core2 =
        TaskSeq.iterAsync (fun v ->
            task {
                do! enqueue core2 (fn v)
            }
        ) (run core) |> ignore
        core2

    let debounce core count (msec : int) =
        let buffer = Array.zeroCreate<'t> count
        let mutable cnt = 0
        let mutable clock = Unchecked.defaultof<Timer>
        let core2 = init<'t[]> 100UL core.ct
        let reset _ =
            valuetask {
                let count = Interlocked.Exchange(&cnt, 0)
                if count > 0 then
                    do! enqueue core2 (Array.take count buffer)  //can overwhelm the actor
                clock.Change(msec,msec*10) |> ignore
            }
            
        do clock <- new Timer(TimerCallback(reset >> ignore))
        
        TaskSeq.iterAsync (fun v ->
            task {
                let count = Interlocked.Increment(&cnt)
                buffer.[count-1] <- v
                if count = buffer.Length then
                    do! enqueue core2 buffer
            }
        ) (run core) |> ignore


    let ingest core count msec (stream : taskSeq<'t>) =
        let buffer = Array.zeroCreate<'t> count
        let mutable cnt = 0
        let mutable clock = Unchecked.defaultof<Timer>
        
        let reset _ =
            let count = Volatile.Read(&cnt)
            if count > 0 then
                enqueue core (Array.take count buffer) |> ignore //can overwhelm the actor
            Interlocked.Exchange(&cnt, 0) |> ignore
            clock.Change(TimeSpan.FromMilliseconds(msec), TimeSpan.MaxValue) |> ignore

        do clock <- new Timer(TimerCallback(reset))

        stream
        |> TaskSeq.iter(fun x -> 
                let c = Interlocked.Increment(&cnt)
                buffer.[c] <- x
                if c = count then
                    reset()
                )
        |> ignore
            
type VACore<'tmsg> = 
    {
        mutable count: uint64
        queueDepth: uint64
        queue: ConcurrentQueue<'tmsg>
        vts: ResettableValueTaskSource
        vtsR: ResettableValueTaskSource
        ct: CancellationToken
    }

[<RequireQualifiedAccess>]    
module AsyncChannels =
    


    let init<'t> qDepth ct =
        let queue = ConcurrentQueue<'t>()
        let vts = ResettableValueTaskSource() //slow down the enqueue (when queue reaches depth)
        let vtsR = ResettableValueTaskSource() //prevents cpu cycling (when queue is empty)
        let mutable count = queue.Count |> uint64
        {
            count = count
            queueDepth = qDepth
            queue = queue
            vts = vts
            vtsR = vtsR
            ct = ct 
        }

    let enqueue (core : VACore<'t>) (item : 't) =
        valuetask {
            core.queue.Enqueue(item)
            let cnt = Threading.Interlocked.Increment(&core.count)
            core.vtsR.SignalWaiter()
                
            if cnt > core.queueDepth then
                core.vts.Reset()
                do! core.vts.WaitAsync(core.ct)
            }
            
    let dequeueM (core : VACore<'t>) =
        if not core.queue.IsEmpty then
            let cnt = core.queue.Count
            let msg = Array.zeroCreate<'tmsg> cnt
            for i=0 to (cnt-1) do
                let isSuccessful, message = core.queue.TryDequeue()
                if isSuccessful then 
                    msg.[i] <- message
                    Threading.Interlocked.Decrement(&core.count) |> ignore
            core.vts.SignalWaiter()
            msg    
        else 
            Array.empty

        
    let dequeue (core : VACore<'t>) =
        let isSuccessful, message = core.queue.TryDequeue()
        if isSuccessful then 
            Threading.Interlocked.Decrement(&core.count)  
            core.vts.SignalWaiter()
            ValueSome(message)  
        else ValueNone 

           

    let ingest (core : VACore<'t[]>) count msec (stream : taskSeq<'t>) =
        let buffer = Array.zeroCreate<'t> count
        let mutable cnt = 0
        let mutable clock = Unchecked.defaultof<Timer>
        
        let reset _ =
            let count = Volatile.Read(&cnt)
            if count > 0 then
                enqueue core (Array.take count buffer) |> ignore //can overwhelm the actor
            Interlocked.Exchange(&cnt, 0) |> ignore
            clock.Change(TimeSpan.FromMilliseconds(msec), TimeSpan.MaxValue) |> ignore

        do clock <- new Timer(TimerCallback(reset))

        stream
        |> TaskSeq.iter(fun x -> 
                let c = Interlocked.Increment(&cnt)
                buffer.[c] <- x
                if c = count then
                    reset()
                )
        |> ignore

    let debounce (core : VACore<'t>) count (msec : int) =
        let buffer = Array.zeroCreate<'t> count
        let mutable cnt = 0
        let mutable clock = Unchecked.defaultof<Timer>
        let core2 = init<'t[]> 100UL core.ct
        let reset _ =
            valuetask {
                let count = Interlocked.Exchange(&cnt, 0)
                if count > 0 then
                    do! enqueue core2 (Array.take count buffer)  //can overwhelm the actor
                clock.Change(msec,msec*10) |> ignore
            }
            
        do clock <- new Timer(TimerCallback(reset >> ignore))

        task {
            while not <| core.ct.IsCancellationRequested do
                match dequeue core with
                | ValueSome(x) ->
                    let c = Interlocked.Increment(&cnt)
                    buffer.[c-1] <- x
                    if c = count then
                        do! reset()
                | ValueNone -> ()
                if core.queue.IsEmpty && Interlocked.Read(&core.count) = 0UL then 
                    core.vtsR.Reset()
                    do! core.vtsR.WaitAsync(core.ct)
            } |> ignore
        core2

    let run (core : VACore<'t>) : taskSeq<'t> =
        taskSeq {
            while not <| core.ct.IsCancellationRequested do
                let dequeue = dequeueM core
                yield! dequeue

                if core.queue.IsEmpty && Interlocked.Read(&core.count) = 0UL then 
                    core.vtsR.Reset()
                    do! core.vtsR.WaitAsync(core.ct)
            }
    
    let iter1 (core : VACore<'t>) (fn : 't -> Task<unit>) =
        task {
            while not <| core.ct.IsCancellationRequested do
                let dequeue = dequeue core
                if dequeue.IsSome then
                    do! fn dequeue.Value
                if core.queue.IsEmpty && Interlocked.Read(&core.count) = 0UL then 
                    core.vtsR.Reset()
                    do! core.vtsR.WaitAsync(core.ct)
        }
    
    let iter (core : VACore<'t>) (fn : 't[] -> ValueTask<unit>)=
        task {
            while not <| core.ct.IsCancellationRequested do
                if core.queue.IsEmpty && Interlocked.Read(&core.count) = 0UL then 
                    
                    core.vtsR.Reset()
                    do! core.vtsR.WaitAsync(core.ct)
                let dequeue = dequeueM core
                do! fn dequeue
        }
        
    let map (core : VACore<'t>) (core2 : VACore<'t2>) (fn : 't -> 't2) =
        task {
            while not <| core.ct.IsCancellationRequested do
                let dequeue = dequeue core
                if dequeue.IsSome then
                    let msg = fn dequeue.Value
                    do! enqueue core2 msg
                if core.queue.IsEmpty && Interlocked.Read(&core.count) = 0UL then 
                    core.vtsR.Reset()
                    do! core.vtsR.WaitAsync(core.ct)
        }
        
    let mult (core : VACore<'t>) =
        let bag = ResizeArray<VACore<'t>>()
        task {
            while not <| core.ct.IsCancellationRequested do
                let dequeue = dequeue core
                if dequeue.IsSome then
                    for x in bag do
                        do! enqueue x dequeue.Value
                if core.queue.IsEmpty && Interlocked.Read(&core.count) = 0UL then 
                    core.vtsR.Reset()
                    do! core.vtsR.WaitAsync(core.ct)
        }
        |> ignore
        fun v -> bag.Add(v)
        
//type VAsyncActor<'tmsg>(qDepth : uint64) as this =

//    let mutable status: int64 = ActorStatus.Stopped
//    let vts = ResettableValueTaskSource() //slow down the enqueue (when queue reaches depth)
//    let vtsR = ResettableValueTaskSource() //prevents cpu cycling (when queue is empty)

//    let queue = ConcurrentQueue<'tmsg>()
//    let mutable count = queue.Count |> uint64
    
//    member this.Dequeue() =
//        valuetask {
//            if not queue.IsEmpty then
//                let cnt = queue.Count
//                let msg = Array.zeroCreate<'tmsg> cnt
//                for i=0 to (cnt-1) do
//                    let isSuccessful, message = queue.TryDequeue()
//                    if isSuccessful then 
//                        msg.[i] <- message
//                        Threading.Interlocked.Decrement(&count) |> ignore
//                if uint64 cnt >= qDepth then
//                   vts.SignalWaiter()
//                return msg    
//            else 
//                return Array.empty
//        }        
            

//    member this.RunS () =
//        if Interlocked.CompareExchange(&status, ActorStatus.Occupied, ActorStatus.Stopped) = ActorStatus.Stopped then
//            taskSeq {
//                while Interlocked.Read(&status) <> ActorStatus.Stopped do
//                    while not queue.IsEmpty do
//                        let isSuccessful, message = queue.TryDequeue()
//                        if isSuccessful then 
                            
//                            yield message
//                            if Threading.Interlocked.Decrement(&count) >= qDepth then
//                                vts.SignalWaiter()
                            
//                    if queue.IsEmpty && Interlocked.Read(&count) = 0UL then 
                        
//                        vtsR.Reset()
//                        do! vtsR.WaitAsync(CancellationToken.None)
//                }
//        else 
//            failwith "Cannot be run concurrently"
    
//    member this.RunA () =
//        if Interlocked.CompareExchange(&status, ActorStatus.Occupied, ActorStatus.Stopped) = ActorStatus.Stopped then
//            taskSeq {
//                while Interlocked.Read(&status) <> ActorStatus.Stopped do
//                    let! dequeue = this.Dequeue()
//                    yield dequeue

//                    if queue.IsEmpty && Interlocked.Read(&count) = 0UL then 
                        
//                        vtsR.Reset()
//                        do! vtsR.WaitAsync(CancellationToken.None)
//                }
//        else 
//            failwith "Cannot be run concurrently"

//    / Enqueue a new messages for the thread to pick up and execute
//    member this.Enqueue(msg: 'tmsg) = //NOT multithread safe
//        valuetask {
//            queue.Enqueue(msg)
//            let cnt =Threading.Interlocked.Increment(&count)
//            if cnt = 1UL then 
//                vtsR.SignalWaiter()
                
//            else if cnt > qDepth then
//                vts.Reset()
//                do! vts.WaitAsync(CancellationToken.None)
//            }
//     Get the length of the actor's message queue
//    member this.QueueCount() = Interlocked.Read(&count)

//     Stops the actor
//    member this.Stop() =
//        Interlocked.Exchange(&status, ActorStatus.Stopped)
//        |> ignore


//    interface IDisposable with
//        member __.Dispose() =
//            this.Stop()


          

//type DebounceQueue<'t>( msec, count, stream : taskSeq<'t>) =
    
//    let buffer = Array.zeroCreate<'t> count
//    let actor = new VAsyncActor<'t[]>(uint64 count)
   
    
//    let mutable cnt = 0
//    let mutable clock = Unchecked.defaultof<Timer>
    
//    let reset _ =
//        let count = Volatile.Read(&cnt)
//        if count > 0 then
//            actor.Enqueue(Array.take count buffer) |> ignore //can overwhelm the actor
//        Interlocked.Exchange(&cnt, 0) |> ignore
//        clock.Change(TimeSpan.FromMilliseconds(msec), TimeSpan.MaxValue) |> ignore

//    do clock <- new Timer(TimerCallback(reset))
    
//    member this.RunTask =
//        stream
//        |> TaskSeq.iter(fun x -> 
//                let c = Volatile.Read(&cnt)
//                buffer.[c] <- x
//                if Interlocked.Increment(&cnt) = count then
//                    reset()
//            )
     
//    member this.Actor = actor 
    

//type Distributer<'t>( queue : ConcurrentQueue<'t>) =

//    let bag = ResizeArray<ConcurrentQueue<'t>>()    
    
//    let th = queue.CreateThreadedProc $"Distributer<{nameof<'t>}>" (fun x -> 
//        bag |> Seq.iter (fun a -> a.Enqueue(x))) CancellationToken.None
        

//    member this.Register(queue) =
//        bag.Add(queue)            
        
        
//type DataFlow =
    

//    static member inline enqueue (actor : ConcurrentQueue<^T>, msg : ^T) :ValueTask<unit> =
//        actor.Enqueue(msg) 
//        Unchecked.defaultof<ValueTask<unit>>

//    static member inline enqueue (actor : ^A, msg : ^T) :ValueTask<unit>  when ^A: (member Enqueue: ^T -> ValueTask<unit> ) =
//        actor.Enqueue(msg) 