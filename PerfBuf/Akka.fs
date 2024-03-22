module PerfBuf.Akka

open Akka
open Akka.Actor
 
type Greet(who) =
    member x.Who = who
 
type GreetingActor() as g =
    inherit ReceiveActor()
    do g.Receive<Greet>(fun (greet:Greet) -> printfn "Hello %s" greet.Who)
 

let main argv = // More details: http://getakka.net/docs/Getting%20started
    let system = ActorSystem.Create "MySystem"
    let greeter = system.ActorOf<GreetingActor> "greeter"
    "World" |> Greet |> greeter.Tell
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code