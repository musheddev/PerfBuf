﻿// For more information see https://aka.ms/fsharp-console-apps

open System
open System.Collections.Concurrent
open System.Diagnostics.Tracing
open System.Runtime.CompilerServices
open System.Security.Cryptography
open System.Threading
open System.Threading.Tasks
open Akka.Actor
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Engines
open BenchmarkDotNet
open BenchmarkDotNet.Environments
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Running
open Disruptor
open FSharp.Control
open Microsoft.Diagnostics.NETCore.Client
open Microsoft.Diagnostics.Tracing.Parsers
open Microsoft.FSharp.Control
open PerfBuf.Benchmark.Util
open Perfolizer.Horology


module Benchmark =


            // this.AddJob(
            //     
            //     Job.Dry
            //         .WithRuntime(CoreRuntime.Latest)
            //         // .WithLaunchCount(1)     // benchmark process will be launched only once
            //         // .WithIterationTime(TimeInterval(nanoseconds = 100000.0)) // 100ms per iteration
            //         // .WithWarmupCount(3)     // 3 warmup iteration
            //         // .WithIterationCount(5)
            //         .WithStrategy(RunStrategy.Monitoring)
            //     )
            // |> ignore
    
    //[<InProcess>]
    // [<SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net70)>]
    // [<MemoryDiagnoser>]
    // [<ThreadingDiagnoser>]
    // [<KeepBenchmarkFiles(true)>]
    //[<EtwProfiler>]
    [<Config(typeof<CustomConfig>)>]
    //[<SimpleJob(RunStrategy.Monitoring,  RuntimeMoniker.Net80, warmupCount = 0, iterationCount = 1, invocationCount = 5, id = "MonitoringJob")>] // cold
    
    //[<SimpleJob(RunStrategy.Monitoring,  RuntimeMoniker.Net80, warmupCount = 5, iterationCount = 10, invocationCount = 1, id = "MonitoringJob2")>] // hot
    [<SimpleJob(RunStrategy.Throughput,  RuntimeMoniker.NativeAot80, invocationCount = 1, id = "InjectingJob")>] // hot
    type SimpleBench1 () =
        
        let mutable hammerFn = fun _ -> ()
        let mutable sw = SpicyWait()
        //let signal = new ManualResetEventSlim(false)
        member val Messages = [||] with get, set

        [<Params(100000)>]
        member val  N : int = 0 with get, set 
            
        [<GlobalSetup>]
        member this.GlobalSetup() = 
            this.Messages <- Array.init this.N (fun x -> Array.init 100 id)
            this.HotHammer()
        member this.HotHammer() =
            
            let spin = AggressiveSpinWait()
            let fn1 () =
                while true do 
                    sw.Wait(0UL)
                    for i in 0 .. this.Messages.Length - 1 do
                        hammerFn(this.Messages.[i])
                    sw.Reset()
            let th = Thread(ThreadStart(fn1))
            th.Start()
        
        member this.StartHammer(fn) =
            hammerFn <- fn
            sw.Signal()

        
        [<Benchmark>]
        member this.Hammer () =
            let q = ConcurrentQueue<int[]>()
            this.StartHammer(q.Enqueue)
            sw.Wait(1UL)

        
        [<Benchmark>]
        member this.Payload () =
            let q = ConcurrentQueue<int[]>()
            this.StartHammer(q.Enqueue)
            sw.Wait(1UL)
            let mutable cnt = 0
            for i in 0 .. this.Messages.Length - 1 do
                cnt <- cnt + 1
                let isSuccessful, x = q.TryDequeue()
                if cnt < this.Messages.Length then
                    let work = Array.sum x
                    ()
            
        [<Benchmark>]
        member this.ThreadedActor () =
           let signal = SpicyWait()
           let mutable cnt = 0
           let actor = PerfBuf.ThreadedActor.ThreadedActor<int[]>(
               fun (x,th) ->
                   cnt <- cnt + 1
                   if cnt < this.Messages.Length then
                       
                       let work = Array.sum x
                       ()
                   else
                       signal.Signal())
           this.StartHammer(actor.Enqueue)
           // for i in 0 .. this.Messages.Length - 1 do
           //      actor.Enqueue(this.Messages.[i])
           signal.Wait(0UL)
           //(actor :> IDisposable).Dispose()
           if (cnt <> this.Messages.Length) then failwithf "quit early %d" cnt

        [<Benchmark>]
        member this.ThreadedActor3 () =
           let signal = SpicyWait()
           let cts = new CancellationTokenSource()
           let mutable cnt = 0
           let actor = PerfBuf.ThreadedActor.ThreadedActor3<int[]>(
               fun x ->
                       cnt <- cnt + 1
                       if cnt < this.Messages.Length then
                           let work = Array.sum x
                           ()
                       else
                           signal.Signal()
               , cts.Token)
           this.StartHammer(actor.Enqueue)
           // for i in 0 .. this.Messages.Length - 1 do
           //      actor.Enqueue(this.Messages.[i])
           signal.Wait(0UL)
           //cts.Cancel()
           //(actor :> IDisposable).Dispose()
           if (cnt <> this.Messages.Length) then failwithf "quit early %d" cnt
        
        [<Benchmark(Baseline = true)>]
        member this.MailBoxProcessor () =
            let signal = SpicyWait()
            let mb = MailboxProcessor(fun mb ->
                let mutable cnt = 0
                let rec loop() = async {
                    let! msg = mb.Receive()
                    cnt <- cnt + 1
                    if cnt < this.Messages.Length then
                        let work = Array.sum msg
                        return! loop()
                    else
                        signal.Signal()
                }
                loop()
                
                )
            mb.Start()
            this.StartHammer(mb.Post)
            // for i in 0 .. this.Messages.Length - 1 do
            //     mb.Post(this.Messages.[i])
            signal.Wait(0UL)



    
    [<Config(typeof<CustomConfig>)>]
    //[<SimpleJob(RunStrategy.Monitoring,  RuntimeMoniker.Net80, warmupCount = 0, iterationCount = 1, invocationCount = 5, id = "MonitoringJob")>] // cold
    
    [<SimpleJob(RunStrategy.Monitoring,  RuntimeMoniker.Net80, warmupCount = 5, iterationCount = 10, invocationCount = 1, id = "MonitoringJob")>] // hot
    //[<SimpleJob(RunStrategy.Throughput,  RuntimeMoniker.Net80, invocationCount = 1, id = "ThroughputJob")>] 
    type MaxConcurrency () =
        
        member val Hammers = [||] with get, set

        member val Messages = [||] with get, set

        [<Params(100,20,10)>]
        member val  T : int = 0 with get, set 
            
        [<Params(100000,1000)>]
        member val  N : int = 0 with get, set
        
        [<Params(1000,100,10)>]
        member val  P : int = 0 with get, set     
            
        [<GlobalSetup>]
        member this.GlobalSetup() = 
            this.Messages <- Array.init this.N (fun x -> Array.init this.P id)
            this.Hammers <- Array.init 1 (fun x -> Hammer(this.Messages))

        [<GlobalCleanup>]
        member this.GlobalCleanup() = 
            for h in this.Hammers do
                h.Stop()
            

        [<Benchmark(Baseline = true)>]
        member this.ThreadedActor3 () =
           let signal = SpicyWait()
           let cts = new CancellationTokenSource()
           
           let actors =
               Array.init this.T (fun x ->
                   let mutable cnt = 0
                   PerfBuf.ThreadedActor.ThreadedActor3<int[]>(
                       fun x ->
                               cnt <- cnt + 1
                               if cnt < this.Messages.Length then
                                   let work = Array.sum x
                                   ()
                               else
                                   signal.Inc()
                       , cts.Token))
           let fn x =
               for a in actors do
                   a.Enqueue(x)
           this.Hammers.[0].StartHammer(fn)

           signal.WaitUntil(uint64 this.T )
           cts.Cancel()
           //(actor :> IDisposable).Dispose()
           //if (cnt <> this.Messages.Length) then failwithf "quit early %d" cnt
        
        [<Benchmark>]
        member this.MailBoxProcessor () =
            let signal = SpicyWait()
            let mbs =
               Array.init this.T (fun x ->
                    MailboxProcessor(fun mb ->
                        let mutable cnt = 0
                        let rec loop() = async {
                            let! msg = mb.Receive()
                            cnt <- cnt + 1
                            if cnt < this.Messages.Length then
                                let work = Array.sum msg
                                return! loop()
                            else
                                signal.Inc()
                        }
                        loop()
                        
                        ))
            for mb in mbs do
                mb.Start()
            let fn x =
               for a in mbs do
                   a.Post(x)
            this.Hammers.[0].StartHammer(fn)
            // for i in 0 .. this.Messages.Length - 1 do
            //     mb.Post(this.Messages.[i])
            signal.WaitUntil(uint64 this.T )

    
    open System.Threading.Channels   
    [<Config(typeof<CustomConfig>)>]
    [<SimpleJob(RunStrategy.Monitoring,  RuntimeMoniker.Net80, warmupCount = 5, iterationCount = 10, invocationCount = 1, id = "MonitoringJob")>] // hot
    //[<SimpleJob(RunStrategy.Throughput,  RuntimeMoniker.Net80, invocationCount = 1, id = "ThroughputJob")>] 
    type ChannelsConcurrency () =
        
        member val Hammers = [||] with get, set

        member val Messages = [||] with get, set

        [<Params(100,20,1)>]
        member val  T : int = 0 with get, set 
            
        [<Params(100000)>]
        member val  N : int = 0 with get, set
        
        [<Params(true,false)>]
        member val  P : bool = true with get, set     
            
        [<GlobalSetup>]
        member this.GlobalSetup() = 
            this.Messages <- Array.init this.N (fun x -> Array.init 100 (fun x -> byte (x % 256)))
            this.Hammers <- Array.init 1 (fun x -> Hammer(this.Messages))

        [<GlobalCleanup>]
        member this.GlobalCleanup() = 
            for h in this.Hammers do
                h.Stop()
            

        [<Benchmark(Baseline = true)>]
        member this.ThreadedActor3 () =
           let signal = SpicyWait()
           let cts = new CancellationTokenSource()
           
           let actors =
               Array.init this.T (fun x ->
                   let mutable cnt = 0
                   let hash = new SHA512Managed()
                   PerfBuf.ThreadedActor.ThreadedActor3<byte[]>(
                       fun x ->
                               cnt <- cnt + 1
                               if cnt < this.Messages.Length then
                                   if this.P then
                                       let work = hash.ComputeHash(x)
                                       ()
                                   else
                                       let work = Array.sumBy (int) x
                                       ()
                               else
                                   signal.Inc()
                       , cts.Token))
           let fn x =
               for a in actors do
                   a.Enqueue(x)
           this.Hammers.[0].StartHammer(fn)

           signal.WaitUntil(uint64 this.T )
           cts.Cancel()
           //(actor :> IDisposable).Dispose()
           //if (cnt <> this.Messages.Length) then failwithf "quit early %d" cnt
        
        [<Benchmark>]
        member this.Channel () =
            let signal = SpicyWait()

            let mbs =
               Array.init this.T (fun x ->
                    let c =  Channel.CreateUnbounded<byte[]>()
                    let mutable cnt = 0
                    let hash = new SHA512Managed()
                    task {
                        while c.Reader.Completion.IsCompleted = false do
                            let! msg = c.Reader.ReadAsync()
                            cnt <- cnt + 1
                            if cnt < this.Messages.Length then
                                  if this.P then
                                       let work = hash.ComputeHash(msg)
                                       ()
                                   else
                                       let work = Array.sumBy (int) msg
                                       ()
                            else
                                signal.Inc()
                        
                    }
                    c
                   )

            let fn x =
               for a in mbs do
                   if not <| a.Writer.TryWrite(x) then failwithf "bad channel"

            this.Hammers.[0].StartHammer(fn)
            // for i in 0 .. this.Messages.Length - 1 do
            //     mb.Post(this.Messages.[i])
            signal.WaitUntil(uint64 this.T )


    [<Config(typeof<CustomConfig>)>]
    [<SimpleJob(RunStrategy.Monitoring,  RuntimeMoniker.Net80, warmupCount = 5, iterationCount = 10, invocationCount = 1, id = "MonitoringJob")>] // hot
    //[<SimpleJob(RunStrategy.Throughput,  RuntimeMoniker.Net80, invocationCount = 1, id = "ThroughputJob")>] 
    type BoundedConcurrency () =
        
        member val Hammers = [||] with get, set

        member val Messages = [||] with get, set

        [<Params(30,15,1)>]
        member val  T : int = 0 with get, set 
            
        [<Params(100000)>]
        member val  N : int = 0 with get, set
        
        [<Params(true,false)>]
        member val  P : bool = true with get, set     
            
        [<GlobalSetup>]
        member this.GlobalSetup() = 
            this.Messages <- Array.init this.N (fun x -> Array.init 100 (fun x -> byte (x % 256)))
            this.Hammers <- Array.init this.T (fun x -> Hammer(this.Messages))

        [<GlobalCleanup>]
        member this.GlobalCleanup() = 
            for h in this.Hammers do
                h.Stop()
            

        [<Benchmark(Baseline = true)>]
        member this.ThreadedActor4 () =
           let signal = SpicyWait()
           let cts = new CancellationTokenSource()
           
           let actors =
               Array.init this.T (fun x ->
                   let mutable cnt = 0
                   let hash = new SHA512Managed()

                   PerfBuf.ThreadedActor.ThreadedActor4<byte[]>(
                       fun x ->
                               cnt <- cnt + 1
                               if cnt < this.Messages.Length then
                                   if this.P then
                                       let work = hash.ComputeHash(x)
                                       ()
                                   else
                                       let work = Array.sumBy (int) x
                                       ()
                               else
                                   signal.Inc()
                       , 100UL
                       , cts.Token))
           Array.iteri (fun i (x : PerfBuf.ThreadedActor.ThreadedActor4<byte[]>) -> this.Hammers.[i].StartHammer(x.Enqueue)) actors

           signal.WaitUntil(uint64 this.T )
           cts.Cancel()
           //(actor :> IDisposable).Dispose()
           //if (cnt <> this.Messages.Length) then failwithf "quit early %d" cnt
        
        [<Benchmark>]
        member this.BoundedChannel () =
            let signal = SpicyWait()

            let mbs =
               Array.init this.T (fun x ->
                    let c =  Channel.CreateBounded<byte[]>(100)
                    let mutable cnt = 0
                    let hash = new SHA512Managed()
                    task {
                        while c.Reader.Completion.IsCompleted = false do
                            let! msg = c.Reader.ReadAsync()
                            cnt <- cnt + 1
                            if cnt < this.Messages.Length then
                                  if this.P then
                                       let work = hash.ComputeHash(msg)
                                       ()
                                   else
                                       let work = Array.sumBy (int) msg
                                       ()
                            else
                                signal.Inc()
                        
                    }
                    c
                   )
            Array.iteri (fun i (x : Channel<byte[]>) -> this.Hammers.[i].StartHammer(fun z -> x.Writer.WriteAsync(z).AsTask().Wait())) mbs
            // let fn x =
            //    for a in mbs do
            //        if not <| a.Writer.TryWrite(x) then failwithf "bad channel"
            //
            // this.Hammers.[0].StartHammer(fn)
            // for i in 0 .. this.Messages.Length - 1 do
            //     mb.Post(this.Messages.[i])
            signal.WaitUntil(uint64 this.T )

    [<Config(typeof<CustomConfig>)>]
    [<SimpleJob(RunStrategy.Monitoring,  RuntimeMoniker.Net80, warmupCount = 3, iterationCount = 5, invocationCount = 1, id = "AsyncChannelsJob")>] // hot
    //[<SimpleJob(RunStrategy.Throughput,  RuntimeMoniker.Net80, invocationCount = 1, id = "ThroughputJob")>] 
    type AsyncChannelsConcurrency () =
        
        
        member val Hammers = [||] with get, set

        member val Messages = [||] with get, set

        [<Params(30,15,1)>]
        member val  T : int = 0 with get, set 
            
        [<Params(100000)>]
        member val  N : int = 0 with get, set
        
        [<Params(true,false)>]
        member val  P : bool = true with get, set     
            
        [<GlobalSetup>]
        member this.GlobalSetup() = 
            this.Messages <- Array.init this.N (fun x -> Array.init 100 (fun x -> byte (x % 256)))
            this.Hammers <- Array.init this.T (fun x -> Hammer(this.Messages))
           

        [<GlobalCleanup>]
        member this.GlobalCleanup() = 
            for h in this.Hammers do
                h.Stop()
            

        [<Benchmark(Baseline = true)>]
        member this.ThreadedActor4 () =
           let signal = SpicyWait()
           let cts = new CancellationTokenSource()
           
           let actors =
               Array.init this.T (fun x ->
                   let mutable cnt = 0
                   let hash = new SHA512Managed()

                   let actor = PerfBuf.ThreadedActor.AsyncChannels.init<byte[]> 100UL cts.Token
                   let iter1 =
                       PerfBuf.ThreadedActor.AsyncChannels.iter1 actor (fun x ->
                                   
                                   cnt <- cnt + 1
                                   if cnt < this.Messages.Length then
                                       if this.P then
                                           let work = hash.ComputeHash(x)
                                           ()
                                       else
                                           let work = Array.sumBy (int) x
                                           ()
                                   else
                                       signal.Inc()
                                   )
                   actor
                   )
           let fn core x =
               let t = (PerfBuf.ThreadedActor.AsyncChannels.enqueue core x)
               if not <| t.IsCompleted then t.GetAwaiter().GetResult()
           Array.iteri (fun i (x : PerfBuf.ThreadedActor.VACore<byte[]>) -> this.Hammers.[i].StartHammer(fn x)) actors

           signal.WaitUntil(uint64 this.T )
           cts.Cancel()
           //(actor :> IDisposable).Dispose()
           //if (cnt <> this.Messages.Length) then failwithf "quit early %d" cnt
        
        [<Benchmark>]
        member this.BoundedChannel () =
            let signal = SpicyWait()

            let mbs =
               Array.init this.T (fun x ->
                    let c =  Channel.CreateBounded<byte[]>(100)
                    let mutable cnt = 0
                    let hash = new SHA512Managed()
                    task {
                        while c.Reader.Completion.IsCompleted = false do
                            let! msg = c.Reader.ReadAsync()
                            cnt <- cnt + 1
                            if cnt < this.Messages.Length then
                                  if this.P then
                                       let work = hash.ComputeHash(msg)
                                       ()
                                   else
                                       let work = Array.sumBy (int) msg
                                       ()
                            else
                                signal.Inc()
                        
                    } |> ignore
                    c
                   )
            Array.iteri (fun i (x : Channel<byte[]>) -> this.Hammers.[i].StartHammer(fun z -> x.Writer.WriteAsync(z).AsTask().Wait())) mbs
            // let fn x =
            //    for a in mbs do
            //        if not <| a.Writer.TryWrite(x) then failwithf "bad channel"
            //
            // this.Hammers.[0].StartHammer(fn)
            // for i in 0 .. this.Messages.Length - 1 do
            //     mb.Post(this.Messages.[i])
            signal.WaitUntil(uint64 this.T )


[<EntryPoint>]
let main argv =
    let summary =
        BenchmarkRunner.Run<Benchmark.AsyncChannelsConcurrency>()
    printfn "RESULTS: %s" summary.ResultsDirectoryPath
    0 // return an integer exit code
