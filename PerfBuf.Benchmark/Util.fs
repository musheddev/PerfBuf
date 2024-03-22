module PerfBuf.Benchmark.Util

open System
open System.Diagnostics.Tracing
open System.Runtime.CompilerServices
open System.Threading
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Diagnosers
open Disruptor
open Microsoft.Diagnostics.NETCore.Client
open Microsoft.Diagnostics.Tracing.Parsers

type SpicyWait() =
    let mutable m = 0UL
    let spin = AggressiveSpinWait()
    
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.Signal() =
        Volatile.Write(&m, 1UL)
    
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.Inc() =
        Interlocked.Increment(&m) |> ignore

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.Reset() =
        Volatile.Write(&m, 0UL) 
        
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]     
    member this.Wait(c) =
        while Volatile.Read(&m) = c do
            spin.SpinOnce()


    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]     
    member this.WaitUntil(c) =
        while Volatile.Read(&m) < c do
            spin.SpinOnce()
            
            
type CustomConfig() as this = 
    inherit ManualConfig()
    
    let providers =
        [|
            new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Verbose, int64 (ClrTraceEventParser.Keywords.Contention ||| ClrTraceEventParser.Keywords.Threading  ||| ClrTraceEventParser.Keywords.GC ))
            new EventPipeProvider("System.Buffers.ArrayPoolEventSource", EventLevel.Informational, Int64.MaxValue)
        |]
    
    do
        //base.Add(DefaultConfig.Instance)
        this.KeepBenchmarkFiles(true)
        this.AddDiagnoser(ThreadingDiagnoser.Default)
        this.AddDiagnoser(new EventPipeProfiler(providers = providers))
        this.AddDiagnoser(MemoryDiagnoser.Default)
        ()
            
            
type Hammer<'t>(messages: 't[][]) =
    let mutable hammerFn = fun _ -> ()
    let mutable sw = SpicyWait()

    let mutable cond = 1

    let fn1 () =
        while cond = 1 do 
            sw.Wait(0UL)
            if cond = 1 then
                for i in 0 .. messages.Length - 1 do
                    hammerFn(messages.[i])
                sw.Reset()
    let th = Thread(ThreadStart(fn1))
    do th.Start()
    

    member this.StartHammer(fn) =
        hammerFn <- fn
        sw.Signal()

    member this.Stop() =
        Interlocked.Exchange(&cond,0)
        sw.Signal()
        th.Join()