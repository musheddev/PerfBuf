# PerfBuf

// * Summary *

BenchmarkDotNet v0.13.8, Windows 10 (10.0.19045.3693/22H2/2022Update)
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
[Host]        : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2 DEBUG
MonitoringJob : .NET 6.0.25 (6.0.2523.51912), X64 RyuJIT AVX2

Job=MonitoringJob  Runtime=.NET 6.0  IterationCount=10  
RunStrategy=Monitoring  WarmupCount=1

| Method           | N     | Mean     | Error     | StdDev    | Ratio | RatioSD | Completed Work Items | Lock Contentions | Allocated  | Alloc Ratio |
|----------------- |------ |---------:|----------:|----------:|------:|--------:|---------------------:|-----------------:|-----------:|------------:|
| ThreadedActor    | 10000 | 1.436 ms | 0.1304 ms | 0.0862 ms |  0.42 |    0.03 |                    - |                - |  571.34 KB |        0.10 |
| MailBoxProcessor | 10000 | 3.460 ms | 0.1054 ms | 0.0697 ms |  1.00 |    0.00 |               1.0000 |           6.0000 | 5814.37 KB |        1.00 |


// * Summary *

BenchmarkDotNet v0.13.8, Windows 10 (10.0.19045.3693/22H2/2022Update)
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
[Host]         : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2 DEBUG
InjectingJob   : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
MonitoringJob2 : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

Runtime=.NET 8.0  InvocationCount=1  UnrollFactor=1

| Method           | Job            | IterationCount | RunStrategy | WarmupCount | N      | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0      | Completed Work Items | Lock Contentions | Allocated | Alloc Ratio |
|----------------- |--------------- |--------------- |------------ |------------ |------- |----------:|----------:|----------:|------:|--------:|----------:|---------------------:|-----------------:|----------:|------------:|
| Hammer           | InjectingJob   | Default        | Throughput  | Default     | 100000 |  1.525 ms | 0.0926 ms | 0.2730 ms |  0.05 |    0.01 |         - |                    - |                - |      2 MB |        0.04 |
| Payload          | InjectingJob   | Default        | Throughput  | Default     | 100000 |  6.702 ms | 0.1874 ms | 0.5408 ms |  0.24 |    0.02 |         - |                    - |                - |      2 MB |        0.04 |
| ThreadedActor    | InjectingJob   | Default        | Throughput  | Default     | 100000 | 11.309 ms | 0.2252 ms | 0.4118 ms |  0.38 |    0.02 |         - |                    - |                - |   5.06 MB |        0.09 |
| ThreadedActor3   | InjectingJob   | Default        | Throughput  | Default     | 100000 |  6.029 ms | 0.1201 ms | 0.3100 ms |  0.21 |    0.01 |         - |                    - |           3.0000 |      2 MB |        0.04 |
| MailBoxProcessor | InjectingJob   | Default        | Throughput  | Default     | 100000 | 29.356 ms | 0.5870 ms | 1.4288 ms |  1.00 |    0.00 | 6000.0000 |               1.0000 |         100.0000 |  56.26 MB |        1.00 |
|                  |                |                |             |             |        |           |           |           |       |         |           |                      |                  |           |             |
| Hammer           | MonitoringJob2 | 10             | Monitoring  | 5           | 100000 |  1.388 ms | 0.4553 ms | 0.3012 ms |  0.04 |    0.01 |         - |                    - |                - |      2 MB |        0.04 |
| Payload          | MonitoringJob2 | 10             | Monitoring  | 5           | 100000 |  6.199 ms | 0.8283 ms | 0.5479 ms |  0.20 |    0.02 |         - |                    - |                - |      2 MB |        0.04 |
| ThreadedActor    | MonitoringJob2 | 10             | Monitoring  | 5           | 100000 | 11.625 ms | 1.5954 ms | 1.0553 ms |  0.38 |    0.05 |         - |                    - |                - |   5.06 MB |        0.09 |
| ThreadedActor3   | MonitoringJob2 | 10             | Monitoring  | 5           | 100000 |  6.202 ms | 1.1018 ms | 0.7287 ms |  0.20 |    0.03 |         - |                    - |           2.0000 |      2 MB |        0.04 |
| MailBoxProcessor | MonitoringJob2 | 10             | Monitoring  | 5           | 100000 | 30.784 ms | 3.3287 ms | 2.2017 ms |  1.00 |    0.00 | 6000.0000 |               1.0000 |          69.0000 |  56.26 MB |        1.00 |


The Internal compute payload is the Payload time - Hammer time (6.702 - 1.525 = 5.177 ms)
Which means that the ThreadedActor3 overhead is 6.029 - 5.177 = 0.852 ms for a 100k messages compared to TheadedActor overhead of 11.309 - 5.177 = 6.132 ms for a 100k messages, a 7.2x improvement.
And a 29.356 - 5.177 = 24.179 ms for a 100k messages for MailBoxProcessor, a 28.3x improvement.