module PerfBuf.Disruptor


open System.Threading
open Disruptor
open Disruptor.Processing
open Disruptor.Dsl
open System
open System.Threading.Tasks


open FSharp.Control

let mkRingBuffer<'t when 't : not struct> (size : int) (factory : unit -> 't) (sync : SynchronizationContext option)=
    let taskScheduler = TaskScheduler.Default
    let s = SequencerFactory.Create(ProducerType.Single, size, AsyncWaitStrategy())
    let r = RingBuffer<'t>(factory, s)
    r

    
    //let disruptor = Disruptor<'t>(factory, 4096, taskScheduler, ProducerType.Single, AsyncWaitStrategy())
    //disruptor

let mkAsyncConsumer<'t when 't : not struct> (fn : EventBatch<'t> -> ValueTask) (ringBuffer : RingBuffer<'t>) (sync : SynchronizationContext) =
    let b = ringBuffer.NewAsyncBarrier()
    let _sq = new Sequence()
    let mutable nextSequence = _sq.Value + 1L;
    let mutable availableSequence = _sq.Value;
    ringBuffer.AddGatingSequences(_sq)
    task {
        try
            SynchronizationContext.SetSynchronizationContext(sync)
            while true do 
                let! batch = b.WaitForAsync(nextSequence)
                if (availableSequence >= nextSequence) then
        
                    let batch = ringBuffer.GetBatch(nextSequence, availableSequence);
                    do! fn(batch)
                    nextSequence <- nextSequence + (int64 batch.Length)
    
                _sq.SetValue(nextSequence - 1L)
        with e -> printfn "Error: %s" e.Message
    } 
    
let mkSourceStream<'t when 't : not struct> (ringBuffer : RingBuffer<'t>) =
    let stream = ringBuffer.NewAsyncEventStream() 
    stream 

//let mkSinkStream<'t when 't : not struct> (ringBuffer : RingBuffer<'t>) (src : taskSeq<'t>) =
    
//    taskSeq {
//        for item in src do
//            ringBuffer.
//            do! ringBuffer.PublishAsyncEvent(item)
//    }
  
let mkValueRingBuffer<'t when 't :> ValueType and 't : (new: unit -> 't) and 't : struct > (size : int) (sync : SynchronizationContext option) =
    let s = SequencerFactory.Create(ProducerType.Single, size, AsyncWaitStrategy())
    let r = ValueRingBuffer<'t>(s)
    r
    
    
let mkConsumer<'t when 't : not struct> (handler :  IEventHandler<'t> ) (ringBuffer : RingBuffer<'t>) =
    let processor : IEventProcessor<'t> = EventProcessorFactory.Create(ringBuffer, ringBuffer.NewBarrier(), handler)
    ringBuffer.AddGatingSequences(processor.Sequence)
    processor

let mkValueConsumer<'t when 't :> ValueType and 't : (new: unit -> 't) and 't : struct > (handler : IValueEventHandler<'t>) (ringBuffer : ValueRingBuffer<'t>) =
    let processor : IValueEventProcessor<'t> = EventProcessorFactory.Create(ringBuffer, ringBuffer.NewBarrier(), handler)
    ringBuffer.AddGatingSequences(processor.Sequence)
    processor


    
    
let mkSimpleHandler (fn : 't -> unit) =
    {   new IEventHandler<'t> with
            member this.OnEvent(data,_,_) = fn data
    
    }


let mkAsyncHandler (fn : EventBatch<'t> -> ValueTask) =
    {
        new IAsyncBatchEventHandler<'t> with
            member this.OnBatch(batch,_) =
                fn batch
    }