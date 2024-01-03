module PerfBuf.AffineThreadPool

/// The MIT License (MIT)
/// 
/// Copyright (c) Bartosz Sypytkowski <b.sypytkowski@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.


open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent

open System.Threading.Tasks.Sources
open System.Runtime.InteropServices
open System.Runtime.ExceptionServices

//type WorkerAgent(shared: ConcurrentQueue<IThreadPoolWorkItem>) =
//    let personal = ConcurrentQueue<IThreadPoolWorkItem>()
//    let resetEvent = new ManualResetEventSlim(true, spinCount=100)
        
//    let swap (l: byref<'t>, r: byref<'t>) =
//        let tmp = l
//        l <- r
//        r <- tmp
            
//    let loop() =
//        let mutable first = personal
//        let mutable second = shared
//        let mutable counter = 0
//        while true do
//            let mutable item = null
//            if first.TryDequeue(&item) || second.TryDequeue(&item)
//            then item.Execute()
//            else 
//                resetEvent.Wait()
//                resetEvent.Reset()
//            counter <- (counter + 1) % 32
//            if counter = 0 then swap(&first, &second)
                        
//    let thread = new Thread(ThreadStart(loop))
//    member this.Schedule(item) = 
//        personal.Enqueue(item)
//        this.WakeUp()
//    member __.WakeUp() = 
//        if not resetEvent.IsSet then
//            resetEvent.Set()
//    member __.Start() = thread.Start()
//    member __.Dispose() =
//        resetEvent.Dispose()
//        thread.Abort()
//    interface IDisposable with member this.Dispose() = this.Dispose()


    
    module AgentStatus =
    
        [<Literal>]
        let Idle = 0L
    
        [<Literal>]
        let Occupied = 1L
    
        [<Literal>]
        let Stopped = 2L
    
    type ThreadAgent(shared: ConcurrentQueue<IThreadPoolWorkItem>) =
        let personal = ConcurrentQueue<IThreadPoolWorkItem>()
        let resetEvent = new ManualResetEventSlim(true, spinCount=100)

        let mutable status: int64 = AgentStatus.Idle

        let threadFn () =
            Thread.BeginThreadAffinity()
            let mutable counter = 0
            while status <> AgentStatus.Stopped  do
                let mutable item = null
                while not personal.IsEmpty do
                    if personal.TryDequeue(&item) 
                    then item.Execute()
                while personal.IsEmpty && not shared.IsEmpty do
                    if shared.TryDequeue(&item) 
                    then item.Execute()
                if personal.IsEmpty then    
                    resetEvent.Reset()
                    if status <> AgentStatus.Stopped then
                        resetEvent.Wait()
            Thread.EndThreadAffinity()

        let thread =
            Thread(ThreadStart(threadFn), Name = "ThreadAgent")

        member this.Schedule(item) = 
            personal.Enqueue(item)
            this.WakeUp()
          
        
        member __.WakeUp() = 
            if not resetEvent.IsSet then
                resetEvent.Set()
                
        member __.Start() = thread.Start()
        member __.Dispose() =
            Interlocked.Exchange(&status,AgentStatus.Stopped) |> ignore
            thread.Join()
            resetEvent.Dispose()
            
        interface IDisposable with member this.Dispose() = this.Dispose()

    

type ThreadPool(size: int) =

    static let shared = lazy (new ThreadPool(Environment.ProcessorCount))

    let mutable i: int = 0
    let sharedQ = ConcurrentQueue<IThreadPoolWorkItem>()
    let agents: ThreadAgent[] = Array.init size <| fun _ -> new ThreadAgent(sharedQ)
    do 
        for agent in agents do
            agent.Start() 
    
    static member Global with get() = shared.Value

    member this.Queue(fn: unit -> unit) = this.UnsafeQueueUserWorkItem { new IThreadPoolWorkItem with member __.Execute() = fn () }
        
    member this.Queue(affinityId, fn: unit -> unit) = 
        this.UnsafeQueueUserWorkItem ({ new IThreadPoolWorkItem with member __.Execute() = fn () }, affinityId)

    member __.UnsafeQueueUserWorkItem(item) = 
        sharedQ.Enqueue item
        i <- Interlocked.Increment(&i) % size
        agents.[i].WakeUp()

    member this.UnsafeQueueUserWorkItem(item, affinityId) = 
        agents.[affinityId % size].Schedule(item)
        
    member tp.QueueUserWorkItem(fn, s) =
        let affinityId = s.GetHashCode()
        tp.UnsafeQueueUserWorkItem({ new IThreadPoolWorkItem with member __.Execute() = fn s }, affinityId)

    member __.Dispose() = 
        for agent in agents do
            agent.Dispose()
    interface IDisposable with member this.Dispose() = this.Dispose()
    
type AffineSynchronizationContex(affinityId) =
    inherit SynchronizationContext()

    override this.Post(d: SendOrPostCallback, state: obj) =
        ThreadPool.Global.Queue(affinityId, fun () -> SynchronizationContext.SetSynchronizationContext(this); d.Invoke(state))
        
    override this.Send(d: SendOrPostCallback, state: obj) =
        ThreadPool.Global.Queue(affinityId, fun () -> SynchronizationContext.SetSynchronizationContext(this); d.Invoke(state))

let startInContext (sync:SynchronizationContext) work = 
  sync.Send((fun _ -> 
    Async.StartImmediate(work)), null)




//[<Struct>]
//type ManualResetValueTaskSourceCore<'TResult>(affinity : int) =
//    struct 
//        val mutable _continuation : unit -> unit
//        val mutable _continuationState : obj
//        val mutable _error : ExceptionDispatchInfo
//        val mutable _result : 'TResult
//        val mutable _version : int16
//        val mutable _completed : bool
//        val mutable _runContinuationsAsynchronously : bool


//        member this.Reset() = 
//            this._version <- this._version + 1s
//            this._continuation <- Unchecked.defaultof<_>
//            this._continuationState <- Unchecked.defaultof<_>
//            this._error <- Unchecked.defaultof<_>
//            this._result <- Unchecked.defaultof<'Tresult>
//            this._completed <- false
//        member this.SetResult(result : 'TResult) = 
//            this._result <- result
//            this.SignalCompletion ()
//        member this.SetException(error : Exception) = 
//            this._error <- ExceptionDispatchInfo.Capture (error)
//            this.SignalCompletion ()
        
//        member this.Version with get() = this._version
//        member this.GetStatus(token : System.Int16) = 
//            this.ValidateToken (token)
//            if Volatile.Read(&this._continuation) = Unchecked.defaultof<_> || not this._completed
//            then ValueTaskSourceStatus.Pending
//            else 
//                if this._error = null 
//                then ValueTaskSourceStatus.Succeeded
//                else 
//                    if this._error.SourceException :? OperationCanceledException
//                    then ValueTaskSourceStatus.Canceled
//                    else ValueTaskSourceStatus.Faulted

//        [<StackTraceHidden>]
//        member this.GetResult(token : System.Int16) = 
//            if token <> this._version || not this._completed || this._error <> null then
//                if this._error <> null then this._error.Throw()
//                else raise (InvalidOperationException())
//                Unchecked.defaultof<'TResult>
//            else this._result

//        member this.OnCompleted(continuation : Action<obj>, state : obj, token : System.Int16, flags : ValueTaskSourceOnCompletedFlags) = 
//            if continuation = null then
//                ThrowHelper.ThrowArgumentNullException (ExceptionArgument.continuation)
//            this.ValidateToken (token)
//            //if flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext <> 0
//            //then this._capturedContext <- ExecutionContext.Capture ()
//            if flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext <> 0
//            then 
//                if (* ERROR UnknownNode "IsPatternExpressionSyntax" SynchronizationContext.Current is SynchronizationContext sc *) && sc.GetType () <> typeof<SynchronizationContext>
//                then 
//                    this._capturedContext <- 
//                        if (* ERROR UnknownNode "IsPatternExpressionSyntax" _capturedContext is null *)
//                        then sc
//                        else new CapturedSchedulerAndExecutionContext(sc, (this._capturedContext :> ExecutionContext))
//                else 
//                    let mutable (ts : TaskScheduler) = TaskScheduler.Current
//                    if ts <> TaskScheduler.Default
//                    then 
//                        this._capturedContext <- 
//                            if (* ERROR UnknownNode "IsPatternExpressionSyntax" _capturedContext is null *)
//                            then ts
//                            else new CapturedSchedulerAndExecutionContext(ts, (this._capturedContext :> ExecutionContext))
//            let mutable (storedContinuation : System.Nullable<Object>) = this._continuation
//            if (* ERROR UnknownNode "IsPatternExpressionSyntax" storedContinuation is null *)
//            then 
//                this._continuationState <- state
//                storedContinuation <- Interlocked.CompareExchange (this._continuation, continuation, Unchecked.defaultof<_>)
//                if (* ERROR UnknownNode "IsPatternExpressionSyntax" storedContinuation is null *)
//                then ()
//            Debug.Assert ((storedContinuation :? not), Unchecked.defaultof<_>, (sprintf "%O is null" (nameof (storedContinuation))))
//            if not (ReferenceEquals (storedContinuation, ManualResetValueTaskSourceCoreShared.s_sentinel))
//            then ThrowHelper.ThrowInvalidOperationException ()

//            AffineThreadPool.ThreadPool.Global.UnsafeQueueUserWorkItem({ new IThreadPoolWorkItem with member this.Execute () = () })

//            let mutable (capturedContext : System.Nullable<Object>) = this._capturedContext
//            match capturedContext with
//            ``null`` -> 
//                ThreadPool.UnsafeQueueUserWorkItem (continuation, state, true)
//                (* ERROR BreakNotSupported "BreakStatementSyntax" break; *)
//            | ExecutionContext -> 
//                ThreadPool.QueueUserWorkItem (continuation, state, true)
//                (* ERROR BreakNotSupported "BreakStatementSyntax" break; *)
//            | _ -> 
//                ManualResetValueTaskSourceCoreShared.ScheduleCapturedContext (capturedContext, continuation, state)
//                (* ERROR BreakNotSupported "BreakStatementSyntax" break; *)
//        member private this.ValidateToken(token : System.Int16) = 
//            if token <> this._version then raise (InvalidOperationException())
            
//        member private this.SignalCompletion() = 
//            if this._completed then ThrowHelper.ThrowInvalidOperationException ()
//            this._completed <- true
//            let mutable (continuation : Action<obj>) = 
//                match Volatile.Read (this._continuation) with
//                | null -> Interlocked.CompareExchange (this._continuation, ManualResetValueTaskSourceCoreShared.s_sentinel, Unchecked.defaultof<_>)
//                | x -> x
//            if continuation <> null then
//                Unchecked.defaultof<_>
//                Debug.Assert ((continuation <> null), Unchecked.defaultof<_>, (sprintf "%O is null" (nameof (continuation))))

//            s.
//            let mutable (context : System.Nullable<Object>) = this._capturedContext
//            if (* ERROR UnknownNode "IsPatternExpressionSyntax" context is null *)
//            then 
//                if this._runContinuationsAsynchronously
//                then ThreadPool.UnsafeQueueUserWorkItem (continuation, this._continuationState, true)
//                else continuation (this._continuationState)
//            else 
//                if (* ERROR UnknownNode "IsPatternExpressionSyntax" context is ExecutionContext or *)
//                then CapturedSchedulerAndExecutionContext
//            invokeContinuation (continuation, this._continuationState, this._runContinuationsAsynchronously)
//            if 
//            then 
//            else 
//                Debug.Assert ((* ERROR UnknownNode "IsPatternExpressionSyntax" context is TaskScheduler or *), SynchronizationContext, (sprintf "context is %O" context))
//                ManualResetValueTaskSourceCoreShared.ScheduleCapturedContext (context, continuation, this._continuationState)
//    end

[<RequireQualifiedAccess>]
module ValueTaskSourceStatus =
    let [<Literal>] None = 0us 
    let [<Literal>] Awaiting = 1us
    let [<Literal>] Completed = 2us

type ValueTaskSource() =
    
    let mutable _state = ValueTaskSourceStatus.None
    let mutable _valueTaskSource = ManualResetValueTaskSourceCore<System.Boolean>()
    let mutable _cancellationRegistration = Unchecked.defaultof<CancellationTokenRegistration> 
    let mutable _keepAlive = Unchecked.defaultof<GCHandle>

    member this.IsCompleted with get() = Volatile.Read(&_state) =  ValueTaskSourceStatus.Completed
    member this.IsCompletedSuccessfully with get() = this.IsCompleted &&  _valueTaskSource.GetStatus(_valueTaskSource.Version) = ValueTaskSourceStatus.Succeeded
   
    member this.TryInitialize( ?keepAlive : obj, ?cancellationToken : CancellationToken) : ValueTask = 
        let keepAlive = (defaultArg keepAlive) Unchecked.defaultof<_>
        let cancellationToken = (defaultArg cancellationToken) Unchecked.defaultof<_>
        let mutable valueTask = Unchecked.defaultof<ValueTask>
        let success = 
            (lock this) (
                fun () ->
                    _valueTaskSource.Reset()
                    _valueTaskSource.RunContinuationsAsynchronously <- true
                    valueTask <- new ValueTask(this :> IValueTaskSource, _valueTaskSource.Version)
                    if _state = ValueTaskSourceStatus.None then 
                        if cancellationToken.CanBeCanceled then 
                            _cancellationRegistration <- cancellationToken.UnsafeRegister((fun x -> (x :?> ValueTaskSource).TrySetException(new OperationCanceledException(cancellationToken)) |> ignore),this)
                        if keepAlive <> null then
                            //Debug.Assert (not _keepAlive.IsAllocated)
                            _keepAlive <- GCHandle.Alloc (keepAlive)

                        _state <- ValueTaskSourceStatus.Awaiting
                        true
                    else false)
        valueTask

    member private this.TryComplete(?e: Exception) = 
        try
            (lock this) (
                fun () -> 
                    try
                        if _state <>  ValueTaskSourceStatus.Completed
                        then 
                            _state <-  ValueTaskSourceStatus.Completed
                            if e.IsSome then
                                let e2 =
                                    if e.Value.StackTrace = null 
                                    then ExceptionDispatchInfo.SetCurrentStackTrace (e.Value)
                                    else e.Value
                                _valueTaskSource.SetException (e2)
                                false
                            else 
                                _valueTaskSource.SetResult (true)
                                true
                        else false
                    finally
                        if _keepAlive.IsAllocated
                        then _keepAlive.Free ())
        finally
            if _cancellationRegistration <> Unchecked.defaultof<CancellationTokenRegistration>  then _cancellationRegistration.Dispose ()

    member this.TrySetResult() = 
        this.TryComplete()
    member this.TrySetException(e : Exception) = 
        this.TryComplete(e)

    interface IValueTaskSource with
        member this.GetStatus(token : System.Int16) = 
            _valueTaskSource.GetStatus(token) 
        member this.OnCompleted(continuation : Action<obj>, state : obj, token : System.Int16, flags : ValueTaskSourceOnCompletedFlags) = 
            _valueTaskSource.OnCompleted (continuation, state, token, flags)
        member this.GetResult(token : System.Int16) = 
            _valueTaskSource.GetResult (token) |> ignore

//type ResettableValueTaskSource(ts : TaskScheduler) =

//    let mutable _waitSource = ManualResetValueTaskSourceCore<Int64>()
//    let mutable _waitSourceCancellation = Unchecked.defaultof<CancellationTokenRegistration> 
//    let mutable _hasWaiter = 0
   

//    member this.SignalWaiter() = 
//        if Interlocked.Exchange (_hasWaiter, 0) = 1
//        then _waitSource.SetResult (true)
//        ()
//    member private this.CancelWaiter(cancellationToken : CancellationToken) = 
//        Debug.Assert (cancellationToken.IsCancellationRequested)
//        if Interlocked.Exchange (this._hasWaiter, 0) = 1
//        then this._waitSource.SetException (ExceptionDispatchInfo.SetCurrentStackTrace (new OperationCanceledException(cancellationToken)))
//        ()
//    member this.Reset() = 
//        if this._hasWaiter <> 0
//        then raise (new InvalidOperationException("Concurrent use is not supported") :> System.Exception)
//        this._waitSource.Reset ()
//        Volatile.Write (this._hasWaiter, 1)
//    member this.Wait() = 
//        this._waitSource.RunContinuationsAsynchronously <- false
//        (((new ValueTask(this, this._waitSource.Version)).AsTask ()).GetAwaiter ()).GetResult ()
//    member this.WaitAsync(cancellationToken : CancellationToken) = 
//        this._waitSource.RunContinuationsAsynchronously <- true
//        this._waitSourceCancellation <- cancellationToken.UnsafeRegister ()
//        let mutable ( : System.) = Unchecked.defaultof<System.>
//        ((* ERROR UnknownPostfixOperator "!" "PostfixUnaryExpressionSyntax" s! *) :> ResettableValueTaskSource).CancelWaiter (token)
//        this
//        (* ERROR UnknownNode "EmptyStatementSyntax" ; *)
//        new ValueTask(this, this._waitSource.Version)

//    interface IValueTaskSource<'TResult> with
//        member this.GetResult(token : int16) = 
//            Debug.Assert (this._hasWaiter = 0)
//            this._waitSourceCancellation.Dispose ()
//            this._waitSourceCancellation <- default
//            this._waitSource.GetResult (token)

//        member this.GetStatus(token : int16) = 
//               (this._waitSource.GetStatus (token)) :> ValueTaskSourceStatus

//        member this.OnCompleted(continuation : Action<obj>, state : obj, token : System.Int16, flags : ValueTaskSourceOnCompletedFlags) = 
//            this._waitSource.OnCompleted (continuation, state, token, flags)
        


type ResettableValueTaskSource(cancellationToken : CancellationToken) as this =

    let mutable _waitSource = ManualResetValueTaskSourceCore<bool>()
    do _waitSource.RunContinuationsAsynchronously <- false
    let mutable _waitSourceCancellation = cancellationToken.UnsafeRegister((fun x -> (x :?> ResettableValueTaskSource).CancelWaiter(cancellationToken)), this)
    let mutable _hasWaiter = 0
   

    member this.SignalWaiter() = 
        if Interlocked.Exchange(&_hasWaiter, 0) = 1
        then _waitSource.SetResult(true)
        ()
    member private this.CancelWaiter(cancellationToken : CancellationToken) = 
        if Interlocked.Exchange(&_hasWaiter, 0) = 1
        then _waitSource.SetException (ExceptionDispatchInfo.SetCurrentStackTrace (new OperationCanceledException(cancellationToken)))
        ()

    member this.WaitAsync() =
        if _hasWaiter <> 0
        then raise (new InvalidOperationException("Concurrent use is not supported") :> System.Exception)
        _waitSource.Reset ()
        Volatile.Write(&_hasWaiter, 1)
        new ValueTask(this :> IValueTaskSource, _waitSource.Version)
        
    interface IValueTaskSource with
        member this.GetResult(token : int16) = 
            _waitSource.GetResult (token) |> ignore

        member this.GetStatus(token : int16) = 
               (_waitSource.GetStatus (token)) :> ValueTaskSourceStatus

        member this.OnCompleted(continuation : Action<obj>, state : obj, token : System.Int16, flags : ValueTaskSourceOnCompletedFlags) = 
            _waitSource.OnCompleted (continuation, state, token, flags)

    interface IValueTaskSource<bool> with
        member this.GetResult(token : int16) = 
            _waitSource.GetResult (token)

        member this.GetStatus(token : int16) = 
               (_waitSource.GetStatus (token)) :> ValueTaskSourceStatus

        member this.OnCompleted(continuation : Action<obj>, state : obj, token : System.Int16, flags : ValueTaskSourceOnCompletedFlags) = 
            _waitSource.OnCompleted (continuation, state, token, flags)
        
    