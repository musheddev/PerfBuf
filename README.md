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





// * Summary *

BenchmarkDotNet v0.13.8, Windows 10 (10.0.19045.3693/22H2/2022Update)
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
[Host]        : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2 DEBUG
MonitoringJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

Job=MonitoringJob  Runtime=.NET 8.0  InvocationCount=1  
IterationCount=10  RunStrategy=Monitoring  UnrollFactor=1  
WarmupCount=5

| Method           | T   | N      | P    | Mean         | Error       | StdDev      | Median       | Ratio | RatioSD | Completed Work Items | Lock Contentions | Gen0         | Gen1      | Allocated     | Alloc Ratio |
|----------------- |---- |------- |----- |-------------:|------------:|------------:|-------------:|------:|--------:|---------------------:|-----------------:|-------------:|----------:|--------------:|------------:|
| ThreadedActor3   | 10  | 1000   | 10   |     2.315 ms |   0.6497 ms |   0.4297 ms |     2.112 ms |  1.00 |    0.00 |                    - |                - |            - |         - |       28.2 KB |        1.00 |
| MailBoxProcessor | 10  | 1000   | 10   |     2.428 ms |   1.0026 ms |   0.6632 ms |     2.262 ms |  1.05 |    0.22 |            3639.0000 |           3.0000 |            - |         - |    7066.55 KB |      250.63 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 10  | 1000   | 100  |     2.212 ms |   0.5752 ms |   0.3805 ms |     2.046 ms |  1.00 |    0.00 |                    - |           1.0000 |            - |         - |      172.9 KB |        1.00 |
| MailBoxProcessor | 10  | 1000   | 100  |     2.136 ms |   1.4116 ms |   0.9337 ms |     1.841 ms |  0.96 |    0.34 |            4954.0000 |           2.0000 |            - |         - |    7582.34 KB |       43.85 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 10  | 1000   | 1000 |     8.149 ms |   0.3290 ms |   0.2176 ms |     8.093 ms |  1.00 |    0.00 |                    - |                - |            - |         - |     335.49 KB |        1.00 |
| MailBoxProcessor | 10  | 1000   | 1000 |     1.506 ms |   0.3322 ms |   0.2197 ms |     1.486 ms |  0.19 |    0.03 |              10.0000 |                - |            - |         - |    5713.92 KB |       17.03 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 10  | 100000 | 10   |    79.828 ms |   3.3985 ms |   2.2479 ms |    80.029 ms |  1.00 |    0.00 |                    - |                - |            - |         - |      110.9 KB |        1.00 |
| MailBoxProcessor | 10  | 100000 | 10   |   448.275 ms |  18.7421 ms |  12.3968 ms |   449.409 ms |  5.62 |    0.18 |          918397.0000 |         124.0000 |  113000.0000 | 1000.0000 |  921897.24 KB |    8,312.99 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 10  | 100000 | 100  |    81.022 ms |   6.2550 ms |   4.1373 ms |    81.861 ms |  1.00 |    0.00 |                    - |                - |            - |         - |     110.88 KB |        1.00 |
| MailBoxProcessor | 10  | 100000 | 100  |   471.477 ms |  15.3807 ms |  10.1734 ms |   471.013 ms |  5.83 |    0.27 |          906593.0000 |         150.0000 |  113000.0000 | 1000.0000 |  917125.01 KB |    8,271.70 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 10  | 100000 | 1000 |    67.962 ms |   6.5265 ms |   4.3169 ms |    66.704 ms |  1.00 |    0.00 |                    - |           2.0000 |            - |         - |   20510.49 KB |        1.00 |
| MailBoxProcessor | 10  | 100000 | 1000 |   137.774 ms |  12.2312 ms |   8.0902 ms |   138.142 ms |  2.04 |    0.19 |           60820.0000 |         105.0000 |   71000.0000 | 1000.0000 |  581056.33 KB |       28.33 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 20  | 1000   | 10   |     4.428 ms |   1.1404 ms |   0.7543 ms |     4.261 ms |  1.00 |    0.00 |                    - |                - |            - |         - |     175.75 KB |        1.00 |
| MailBoxProcessor | 20  | 1000   | 10   |     7.634 ms |   1.0520 ms |   0.6958 ms |     7.649 ms |  1.75 |    0.20 |           16575.0000 |           3.0000 |    2000.0000 |         - |   17769.74 KB |      101.11 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 20  | 1000   | 100  |     4.376 ms |   1.0524 ms |   0.6961 ms |     4.196 ms |  1.00 |    0.00 |                    - |                - |            - |         - |     475.42 KB |        1.00 |
| MailBoxProcessor | 20  | 1000   | 100  |     6.362 ms |   3.9554 ms |   2.6163 ms |     7.219 ms |  1.43 |    0.56 |           12919.0000 |           5.0000 |    2000.0000 |         - |   16328.16 KB |       34.34 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 20  | 1000   | 1000 |    12.577 ms |   7.7220 ms |   5.1076 ms |    15.447 ms |  1.00 |    0.00 |                    - |                - |            - |         - |      200.5 KB |        1.00 |
| MailBoxProcessor | 20  | 1000   | 1000 |     2.725 ms |   0.2587 ms |   0.1711 ms |     2.716 ms |  0.29 |    0.22 |              20.0000 |                - |    1000.0000 |         - |   11316.65 KB |       56.44 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 20  | 100000 | 10   |   106.073 ms |   5.8472 ms |   3.8676 ms |   107.400 ms |  1.00 |    0.00 |                    - |           1.0000 |            - |         - |     394.13 KB |        1.00 |
| MailBoxProcessor | 20  | 100000 | 10   |   995.167 ms |  62.6059 ms |  41.4100 ms |   983.305 ms |  9.39 |    0.43 |         1904052.0000 |         218.0000 |  230000.0000 | 5000.0000 | 1868646.39 KB |    4,741.25 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 20  | 100000 | 100  |   107.022 ms |   5.0454 ms |   3.3372 ms |   107.398 ms |  1.00 |    0.00 |                    - |           1.0000 |            - |         - |      425.9 KB |        1.00 |
| MailBoxProcessor | 20  | 100000 | 100  |   961.085 ms | 101.3586 ms |  67.0424 ms |   929.498 ms |  8.98 |    0.53 |         1898199.0000 |         162.0000 |  230000.0000 | 5000.0000 | 1866210.52 KB |    4,381.82 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 20  | 100000 | 1000 |   114.372 ms |  18.6661 ms |  12.3465 ms |   110.100 ms |  1.00 |    0.00 |                    - |           3.0000 |            - |         - |   34875.02 KB |        1.00 |
| MailBoxProcessor | 20  | 100000 | 1000 |   460.296 ms | 310.3639 ms | 205.2865 ms |   327.800 ms |  4.03 |    1.71 |         1280822.0000 |         184.0000 |  200000.0000 | 1000.0000 | 1621875.97 KB |       46.51 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 100 | 1000   | 10   |    19.155 ms |   3.3623 ms |   2.2239 ms |    19.007 ms |  1.00 |    0.00 |                    - |           1.0000 |            - |         - |     313.09 KB |        1.00 |
| MailBoxProcessor | 100 | 1000   | 10   |    46.211 ms |   8.5105 ms |   5.6292 ms |    43.244 ms |  2.45 |    0.49 |           97762.0000 |           8.0000 |   11000.0000 | 1000.0000 |   94537.21 KB |      301.95 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 100 | 1000   | 100  |    19.627 ms |   3.2340 ms |   2.1391 ms |    20.259 ms |  1.00 |    0.00 |                    - |           1.0000 |            - |         - |     277.61 KB |        1.00 |
| MailBoxProcessor | 100 | 1000   | 100  |    42.819 ms |  10.8558 ms |   7.1804 ms |    43.501 ms |  2.20 |    0.43 |           94584.0000 |           8.0000 |   11000.0000 | 1000.0000 |   93290.13 KB |      336.05 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 100 | 1000   | 1000 |    18.547 ms |   2.6225 ms |   1.7346 ms |    18.824 ms |  1.00 |    0.00 |                    - |           1.0000 |            - |         - |    1725.42 KB |        1.00 |
| MailBoxProcessor | 100 | 1000   | 1000 |    22.304 ms |  24.1514 ms |  15.9747 ms |    11.777 ms |  1.25 |    0.97 |             364.0000 |           4.0000 |    7000.0000 | 1000.0000 |   56986.71 KB |       33.03 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 100 | 100000 | 10   |   281.220 ms |  26.7879 ms |  17.7185 ms |   271.140 ms |  1.00 |    0.00 |                    - |                - |            - |         - |     873.17 KB |        1.00 |
| MailBoxProcessor | 100 | 100000 | 10   | 5,719.388 ms | 479.5971 ms | 317.2238 ms | 5,947.126 ms | 20.37 |    1.14 |         9909878.0000 |         816.0000 | 1171000.0000 | 1000.0000 | 9495717.41 KB |   10,874.97 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 100 | 100000 | 100  |   275.406 ms |  10.3832 ms |   6.8678 ms |   273.359 ms |  1.00 |    0.00 |                    - |                - |            - |         - |     843.91 KB |        1.00 |
| MailBoxProcessor | 100 | 100000 | 100  | 5,587.295 ms |  64.6290 ms |  42.7481 ms | 5,600.417 ms | 20.30 |    0.50 |         9920532.0000 |         836.0000 | 1171000.0000 | 1000.0000 | 9499865.83 KB |   11,256.91 |
|                  |     |        |      |              |             |             |              |       |         |                      |                  |              |           |               |             |
| ThreadedActor3   | 100 | 100000 | 1000 |   473.481 ms |  18.0181 ms |  11.9179 ms |   472.195 ms |  1.00 |    0.00 |                    - |          29.0000 |    1000.0000 |         - |   42750.06 KB |        1.00 |
| MailBoxProcessor | 100 | 100000 | 1000 | 1,497.427 ms | 113.9918 ms |  75.3985 ms | 1,499.822 ms |  3.16 |    0.18 |          165447.0000 |         482.0000 |  691000.0000 | 3000.0000 |  5647372.2 KB |      132.10 |


Question: When do context switches outway costs of async. Perhaps mailbox processer is a not a good choice for this comparision. Lets try with tasks, and Class called Async Channels
To get here we have to dramatically increase the compute playload of SHA512 hash of 100bytes


// * Summary *

BenchmarkDotNet v0.13.8, Windows 10 (10.0.19045.3693/22H2/2022Update)
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
[Host]        : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2 DEBUG
MonitoringJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

Job=MonitoringJob  Runtime=.NET 8.0  InvocationCount=1  
IterationCount=10  RunStrategy=Monitoring  UnrollFactor=1  
WarmupCount=5

| Method         | T   | N      | P   | Mean        | Error      | StdDev     | Ratio | RatioSD | Gen0        | Completed Work Items | Lock Contentions | Gen1      | Gen2      | Allocated  | Alloc Ratio |
|--------------- |---- |------- |---- |------------:|-----------:|-----------:|------:|--------:|------------:|---------------------:|-----------------:|----------:|----------:|-----------:|------------:|
| ThreadedActor3 | 1   | 100000 | 100 |    47.04 ms |   4.391 ms |   2.904 ms |  1.00 |    0.00 |   2000.0000 |                    - |           1.0000 |         - |         - |   18.79 MB |        1.00 |
| Channel        | 1   | 100000 | 100 |    53.52 ms |   8.032 ms |   5.313 ms |  1.14 |    0.13 |   2000.0000 |               1.0000 |                - |         - |         - |   18.79 MB |        1.00 |
|                |     |        |     |             |            |            |       |         |             |                      |                  |           |           |            |             |
| ThreadedActor3 | 20  | 100000 | 100 |   171.67 ms |  13.880 ms |   9.180 ms |  1.00 |    0.00 |  42000.0000 |                    - |                - | 1000.0000 |         - |  375.75 MB |        1.00 |
| Channel        | 20  | 100000 | 100 |   928.83 ms |  26.867 ms |  17.771 ms |  5.42 |    0.31 |  42000.0000 |         1798511.0000 |          21.0000 | 1000.0000 |         - |   336.1 MB |        0.89 |
|                |     |        |     |             |            |            |       |         |             |                      |                  |           |           |            |             |
| ThreadedActor3 | 100 | 100000 | 100 | 1,470.09 ms | 361.771 ms | 239.289 ms |  1.00 |    0.00 | 215000.0000 |                    - |           4.0000 | 5000.0000 | 1000.0000 | 1840.75 MB |        1.00 |
| Channel        | 100 | 100000 | 100 |   913.64 ms | 385.368 ms | 254.897 ms |  0.64 |    0.25 | 215000.0000 |            2245.0000 |           1.0000 | 5000.0000 | 1000.0000 | 1746.27 MB |        0.95 |


// * Summary *

BenchmarkDotNet v0.13.8, Windows 10 (10.0.19045.3693/22H2/2022Update)
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
[Host]        : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2 DEBUG
MonitoringJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

Job=MonitoringJob  Runtime=.NET 8.0  InvocationCount=1  
IterationCount=10  RunStrategy=Monitoring  UnrollFactor=1  
WarmupCount=5

| Method         | T   | N      | P     | Mean         | Error      | StdDev      | Ratio | RatioSD | Gen0        | Completed Work Items | Lock Contentions | Gen1      | Gen2      | Allocated     | Alloc Ratio |
|--------------- |---- |------- |------ |-------------:|-----------:|------------:|------:|--------:|------------:|---------------------:|-----------------:|----------:|----------:|--------------:|------------:|
| ThreadedActor3 | 1   | 100000 | False |     5.894 ms |   1.155 ms |   0.7637 ms |  1.00 |    0.00 |           - |                    - |           2.0000 |         - |         - |    2051.88 KB |        1.00 |
| Channel        | 1   | 100000 | False |    11.862 ms |   1.695 ms |   1.1215 ms |  2.04 |    0.32 |           - |               1.0000 |                - |         - |         - |     2052.4 KB |        1.00 |
|                |     |        |       |              |            |             |       |         |             |                      |                  |           |           |               |             |
| ThreadedActor3 | 1   | 100000 | True  |    49.052 ms |   9.460 ms |   6.2569 ms |  1.00 |    0.00 |   2000.0000 |                    - |           1.0000 |         - |         - |    19239.2 KB |        1.00 |
| Channel        | 1   | 100000 | True  |    52.764 ms |   8.348 ms |   5.5220 ms |  1.10 |    0.21 |   2000.0000 |               1.0000 |                - |         - |         - |   19239.73 KB |        1.00 |
|                |     |        |       |              |            |             |       |         |             |                      |                  |           |           |               |             |
| ThreadedActor3 | 20  | 100000 | False |   102.835 ms |   5.534 ms |   3.6603 ms |  1.00 |    0.00 |           - |                    - |           1.0000 |         - |         - |     422.17 KB |        1.00 |
| Channel        | 20  | 100000 | False |   866.496 ms |  51.184 ms |  33.8551 ms |  8.44 |    0.44 |           - |         1958891.0000 |          12.0000 |         - |         - |     116.91 KB |        0.28 |
|                |     |        |       |              |            |             |       |         |             |                      |                  |           |           |               |             |
| ThreadedActor3 | 20  | 100000 | True  |   170.972 ms |  14.906 ms |   9.8591 ms |  1.00 |    0.00 |  42000.0000 |                    - |                - | 1000.0000 |         - |  384770.88 KB |        1.00 |
| Channel        | 20  | 100000 | True  |   773.445 ms |  28.016 ms |  18.5307 ms |  4.54 |    0.32 |  42000.0000 |         1790185.0000 |          12.0000 | 1000.0000 |         - |  344117.42 KB |        0.89 |
|                |     |        |       |              |            |             |       |         |             |                      |                  |           |           |               |             |
| ThreadedActor3 | 100 | 100000 | False |   269.136 ms |   8.376 ms |   5.5399 ms |  1.00 |    0.00 |           - |                    - |           2.0000 |         - |         - |     833.09 KB |        1.00 |
| Channel        | 100 | 100000 | False | 3,844.147 ms |  49.555 ms |  32.7776 ms | 14.29 |    0.32 |           - |         9970796.0000 |          14.0000 |         - |         - |     278.78 KB |        0.33 |
|                |     |        |       |              |            |             |       |         |             |                      |                  |           |           |               |             |
| ThreadedActor3 | 100 | 100000 | True  | 1,432.064 ms | 341.260 ms | 225.7225 ms |  1.00 |    0.00 | 214000.0000 |                    - |           1.0000 | 2000.0000 |         - |  1897220.5 KB |        1.00 |
| Channel        | 100 | 100000 | True  | 1,135.409 ms | 677.421 ms | 448.0719 ms |  0.79 |    0.30 | 216000.0000 |            2064.0000 |           4.0000 | 8000.0000 | 2000.0000 | 1821499.04 KB |        0.96 |


Here P represents whether it is computing sha512 hash or just a sum. When does the context switching outway the cost of async. Not till 100 threads with the larger compute payload. Or was something else going on here, look like the jit found an optimizatio n path reducing the completed work itmms, which we would expected to be cllose to 10 million. But it still looks like the complete the hash work correctly because the allocation is pretty simular.

So the obvious solution to incraseing efficent use of the thread pool through tasks is with a larger compute payload, thus mini batching may be the way to go. However, too large of batches could clog the thread pool incraseing latency to the rest of the system utilizing the thread pool.


So far the examples have been with unbounded queues, what if we used bounded queues and see if adding a back pressure mechanism makes a difference. 


// * Summary *

BenchmarkDotNet v0.13.8, Windows 10 (10.0.19045.3803/22H2/2022Update)
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
[Host]        : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2 DEBUG
MonitoringJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

Job=MonitoringJob  Runtime=.NET 8.0  InvocationCount=1  
IterationCount=10  RunStrategy=Monitoring  UnrollFactor=1  
WarmupCount=5

| Method         | T  | N      | P     | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0       | Completed Work Items | Lock Contentions | Gen1      | Allocated    | Alloc Ratio |
|--------------- |--- |------- |------ |----------:|----------:|----------:|----------:|------:|--------:|-----------:|---------------------:|-----------------:|----------:|-------------:|------------:|
| ThreadedActor4 | 1  | 100000 | False |  13.20 ms |  0.319 ms |  0.211 ms |  13.23 ms |  1.00 |    0.00 |          - |                    - |                - |         - |      5.73 KB |        1.00 |
| Channel        | 1  | 100000 | False |  23.86 ms |  4.300 ms |  2.844 ms |  23.61 ms |  1.81 |    0.22 |          - |           10512.0000 |           1.0000 |         - |    892.59 KB |      155.87 |
|                |    |        |       |           |           |           |           |       |         |            |                      |                  |           |              |             |
| ThreadedActor4 | 1  | 100000 | True  |  56.45 ms | 10.095 ms |  6.677 ms |  54.00 ms |  1.00 |    0.00 |  2000.0000 |                    - |                - |         - |  17193.05 KB |        1.00 |
| Channel        | 1  | 100000 | True  |  74.40 ms | 14.239 ms |  9.418 ms |  72.67 ms |  1.32 |    0.13 |  2000.0000 |           73682.0000 |           2.0000 |         - |  22973.02 KB |        1.34 |
|                |    |        |       |           |           |           |           |       |         |            |                      |                  |           |              |             |
| ThreadedActor4 | 15 | 100000 | False |  40.93 ms |  5.924 ms |  3.919 ms |  41.67 ms |  1.00 |    0.00 |          - |                    - |           4.0000 |         - |     75.88 KB |        1.00 |
| Channel        | 15 | 100000 | False |  71.77 ms | 51.068 ms | 33.778 ms |  53.28 ms |  1.81 |    0.94 |          - |           66489.0000 |          25.0000 |         - |   4557.92 KB |       60.07 |
|                |    |        |       |           |           |           |           |       |         |            |                      |                  |           |              |             |
| ThreadedActor4 | 15 | 100000 | True  | 153.95 ms | 14.809 ms |  9.795 ms | 154.46 ms |  1.00 |    0.00 | 31000.0000 |                    - |          17.0000 |         - | 257885.78 KB |        1.00 |
| Channel        | 15 | 100000 | True  | 136.68 ms | 10.014 ms |  6.624 ms | 135.38 ms |  0.89 |    0.06 | 33000.0000 |          170032.0000 |          10.0000 |         - | 271112.13 KB |        1.05 |
|                |    |        |       |           |           |           |           |       |         |            |                      |                  |           |              |             |
| ThreadedActor4 | 30 | 100000 | False |  68.97 ms |  9.827 ms |  6.500 ms |  71.24 ms |  1.00 |    0.00 |          - |                    - |           1.0000 |         - |    151.33 KB |        1.00 |
| Channel        | 30 | 100000 | False |  73.10 ms | 37.890 ms | 25.062 ms |  65.95 ms |  1.07 |    0.37 |          - |           87009.0000 |          22.0000 |         - |   4806.56 KB |       31.76 |
|                |    |        |       |           |           |           |           |       |         |            |                      |                  |           |              |             |
| ThreadedActor4 | 30 | 100000 | True  | 299.31 ms | 25.703 ms | 17.001 ms | 299.14 ms |  1.00 |    0.00 | 64000.0000 |                    - |          30.0000 |         - | 515771.48 KB |        1.00 |
| Channel        | 30 | 100000 | True  | 237.35 ms | 15.009 ms |  9.927 ms | 239.31 ms |  0.80 |    0.07 | 65000.0000 |           76520.0000 |          48.0000 | 1000.0000 | 520281.57 KB |        1.01 |



# REFERENCE MATERIAL
https://github.com/fsprojects/FSharp.Control.TaskSeq
https://www.bartoszsypytkowski.com/writing-high-performance-f-code/
https://hamidmosalla.com/2018/06/24/what-is-synchronizationcontext/
https://mechanical-sympathy.blogspot.com/
https://github.com/disruptor-net/Disruptor-net
https://adamsitnik.com/Array-Pool/
https://matthewcrews.com/blog/2022/03/performance-of-key-value-lookups-types/
https://matthewcrews.com/blog/2022/02/high-performance-observation-tracking/
https://matthewcrews.com/blog/2022/03/performance-of-dus-and-active-patterns/

https://medium.com/@SajadJ/c-concurrency-a-note-to-myself-8a99664057bd
https://neuecc.medium.com/how-to-make-the-fastest-net-serializer-with-net-7-c-11-case-of-memorypack-ad28c0366516

https://github.com/dotnet/corefx/blob/master/src/System.Net.Sockets/src/System/Net/Sockets/Socket.Tasks.cs#L808-L1097
https://blog.scooletz.com/2018/05/14/task-async-await-valuetask-ivaluetasksource-and-how-to-keep-your-sanity-in-modern-net-world/
https://github.com/dotnet/runtime/blob/c98c9dc5d9017762c33b449a75c8392ddb10cbf4/src/libraries/System.Net.Quic/src/System/Net/Quic/Internal/ValueTaskSource.cs#L26

https://gist.github.com/Horusiath/70ef44c379257841b645dbafe21e1ae9?ref=bartoszsypytkowski.com
https://www.bartoszsypytkowski.com/thread-safety-with-affine-thread-pools/
https://hamidmosalla.com/2023/04/29/thread-affinity-in-parallel-programming-using-tpl/
https://stackoverflow.com/questions/65529509/what-is-icriticalnotifycompletion-for
https://ferrous-systems.com/blog/lock-free-ring-buffer/

https://stackoverflow.com/questions/65890049/f-synchronously-start-async-within-a-synchronizationcontext
https://learn.microsoft.com/en-us/dotnet/api/system.io.pipelines.pipewriter?view=dotnet-plat-ext-7.0&viewFallbackFrom=net-7.0
https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraybufferwriter-1?view=net-7.0
https://www.fssnip.net/Q/title/Bit-manipulation-methods
https://medium.com/@epeshk/the-big-performance-difference-between-arraypools-in-net-b25c9fc5e31d
https://github.com/YellPika/FsChannel/blob/master/FsChannel.Demo/Program.fs
https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector-1?view=net-8.0
https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/byrefs

https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
https://blog.stephencleary.com/2023/11/configureawait-in-net-8.html
