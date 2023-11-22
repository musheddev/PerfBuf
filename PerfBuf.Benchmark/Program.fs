// For more information see https://aka.ms/fsharp-console-apps

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Engines
open BenchmarkDotNet
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Running

module Benchmark =

    //[<InProcess>]
    [<SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net70)>]
    [<MemoryDiagnoser>]
    [<ThreadingDiagnoser>]
    [<KeepBenchmarkFiles(true)>]
    //[<EtwProfiler>]
    type SimpleBench1 () =
        
//        member val Messages = [||] with get, set
//        member val Queue : BlockingQueue<string*string> = nul() with get, set
//        member val Handler1 : IMessageHandler = nul() with get, set
//        member val Handler2 : IMessageHandler = nul() with get, set
//        member val Dictionary : MamaDictionary = nul() with get, set
        
        [<GlobalSetup>]
        member this.GlobalSetup() = ()
//            let copy_to = (Directory.GetCurrentDirectory())
//            let copy_from = Environment.GetEnvironmentVariable("BIN_PATH")
//            for path in Directory.GetFiles(copy_from) do
//                let fname = Path.GetFileName(path)
//                File.Copy(path,Path.Combine(copy_to,fname),true)
//                printfn "COPY %s to %s" path (Path.Combine(copy_to,fname))
//            printfn "CURRENT DIRECTORY: %s" (Directory.GetCurrentDirectory())
//            let bridge = Mama.loadBridge "wmw"
//            Mama.openWithProperties(Environment.GetEnvironmentVariable("WOMBAT_PATH"),"mama.properties")
            
            //this.Dictionary <- MamaTools.loadDict()
            //this.Messages <- MamaTools.loadMsgs ("goog_opt_test.json") this.Dictionary
            //this.Queue <- new BlockingQueue<string*string>()
            //this.Handler1 <- new OptionContractMessageHandler.OptionHandler("GOOG__210219P01240000",this.Queue,this.Dictionary)
            //this.Handler2 <- new OptionContractMessageHandlerV2.OptionHandler("GOOG__210219P01240000",this.Queue,this.Dictionary)
        

        
        [<Benchmark>]
        member this.StackEliminated () = ()


        [<Benchmark(Baseline = true)>]
        member this.Current () = ()

        




[<EntryPoint>]
let main argv =
    let summary =
        BenchmarkRunner.Run<Benchmark.SimpleBench1>()
    printfn "RESULTS: %s" summary.ResultsDirectoryPath
    0 // return an integer exit code
