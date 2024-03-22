module PerfBuf.Benchmark.AllBench

    open System.Security.Cryptography
    open System.Threading
    open System.Threading.Channels
    open Akka.Actor
    open BenchmarkDotNet.Attributes
    open BenchmarkDotNet.Engines
    open BenchmarkDotNet.Jobs
    open PerfBuf.Benchmark.Util


    [<Config(typeof<CustomConfig>)>]
    [<SimpleJob(RunStrategy.Monitoring,  RuntimeMoniker.Net80, warmupCount = 3, iterationCount = 5, invocationCount = 1, id = "AsyncJob")>] // hot
    //[<SimpleJob(RunStrategy.Throughput,  RuntimeMoniker.Net80, invocationCount = 1, id = "ThroughputJob")>] 
    type AsyncConcurrency () =
        
        member val System = Unchecked.defaultof<ActorSystem>
        member val Hammers = [||] with get, set

        member val Messages = [||] with get, set

        [<Params(30,15,1)>]
        member val  NumConcurrency : int = 0 with get, set 
            
        [<Params(100000)>]
        member val  NumMessages : int = 0 with get, set
        
        [<Params(true,false)>]
        member val  Payload : bool = true with get, set     
            
        [<GlobalSetup>]
        member this.GlobalSetup() = 
            this.Messages <- Array.init this.NumMessages (fun x -> Array.init 100 (fun x -> byte (x % 256)))
            this.Hammers <- Array.init this.NumConcurrency (fun x -> Hammer(this.Messages))
            this.System = ActorSystem.Create "MySystem"
            
        [<GlobalCleanup>]
        member this.GlobalCleanup() = 
            for h in this.Hammers do
                h.Stop()
            

        [<Benchmark(Baseline = true)>]
        member this.ThreadedActor4 () =
           let signal = SpicyWait()
           let cts = new CancellationTokenSource()
           
           let actors =
               Array.init this.NumConcurrency (fun x ->
                   let mutable cnt = 0
                   let hash = new SHA512Managed()

                   let actor = PerfBuf.ThreadedActor.AsyncChannels.init<byte[]> 100UL cts.Token
                   let iter1 =
                       PerfBuf.ThreadedActor.AsyncChannels.iter1 actor (fun x ->
                                   
                                   cnt <- cnt + 1
                                   if cnt < this.Messages.Length then
                                       if this.Payload then
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

           signal.WaitUntil(uint64 this.NumConcurrency )
           cts.Cancel()
           //(actor :> IDisposable).Dispose()
           //if (cnt <> this.Messages.Length) then failwithf "quit early %d" cnt
        
        [<Benchmark>]
        member this.BoundedChannel () =
            let signal = SpicyWait()

            let mbs =
               Array.init this.NumConcurrency (fun x ->
                    let c =  Channel.CreateBounded<byte[]>(100)
                    let mutable cnt = 0
                    let hash = new SHA512Managed()
                    task {
                        while c.Reader.Completion.IsCompleted = false do
                            let! msg = c.Reader.ReadAsync()
                            cnt <- cnt + 1
                            if cnt < this.Messages.Length then
                                  if this.Payload then
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
            signal.WaitUntil(uint64 this.NumConcurrency )

        [<Benchmark>]
        member this.AkkaStreams () = 