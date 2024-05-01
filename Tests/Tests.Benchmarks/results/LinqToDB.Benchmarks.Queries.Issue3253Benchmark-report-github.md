```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5458/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-GXDOCB : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2
  Job-YDFVLV : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-SBTNYY : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                                                         | Runtime              | Mean            | Allocated |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------:|
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |     98,067.3 ns |   29232 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |     74,079.6 ns |   29576 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |     98,457.6 ns |   29232 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |     90,106.2 ns |   29576 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |    104,602.1 ns |   69952 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    245,804.7 ns |   74520 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |    228,647.8 ns |   69952 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |    224,354.8 ns |   74520 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 63,737,489.2 ns | 1010777 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 63,436,985.3 ns | 1010991 B |
| RawAdoNet                                                                      | .NET 6.0             |        158.5 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |     78,344.9 ns |   24272 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |     81,911.6 ns |   24216 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |     71,489.6 ns |   23936 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |     81,598.7 ns |   24216 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |    183,763.0 ns |   56016 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    172,053.0 ns |   56648 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |    184,968.5 ns |   56016 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |    196,397.0 ns |   56648 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 64,512,630.9 ns |  895104 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 69,982,127.6 ns |  895615 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 6.0             |      4,528.4 ns |    4384 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 6.0             |    216,705.6 ns |   59048 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 6.0             |    220,055.5 ns |   60000 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |     48,985.0 ns |   18496 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     23,074.4 ns |   18552 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |     49,012.4 ns |   19248 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |     51,280.1 ns |   19032 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |    106,152.1 ns |   43872 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |    122,226.0 ns |   43992 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |     53,106.6 ns |   44352 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |    124,990.9 ns |   44456 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 59,439,074.1 ns |  961502 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 60,839,749.2 ns |  959894 B |
| RawAdoNet                                                                      | .NET 8.0             |        111.1 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |     17,047.2 ns |   15888 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     37,648.8 ns |   16168 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |     38,553.1 ns |   16416 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |     39,909.7 ns |   16328 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |     41,765.9 ns |   37456 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     99,638.8 ns |   37576 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |     97,019.5 ns |   37552 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |     97,618.3 ns |   38088 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 65,763,439.3 ns |  852600 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 64,962,883.5 ns |  850458 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 8.0             |      3,741.7 ns |    4384 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 8.0             |    115,590.8 ns |   40680 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 8.0             |    120,897.4 ns |   40640 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    152,667.3 ns |   32408 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    174,084.4 ns |   32914 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    161,894.6 ns |   32408 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    177,256.0 ns |   32914 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    382,888.1 ns |   79313 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    385,808.0 ns |   76361 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    385,353.9 ns |   79313 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    387,327.2 ns |   76361 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 67,741,028.3 ns | 1120271 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 67,280,017.5 ns | 1118278 B |
| RawAdoNet                                                                      | .NET Framework 4.6.2 |        460.3 ns |     417 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    126,015.2 ns |   25966 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    136,942.6 ns |   27244 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    123,683.5 ns |   25972 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    134,846.0 ns |   26860 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    285,822.1 ns |   60301 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    277,505.4 ns |   61189 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    240,390.6 ns |   60295 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    274,374.5 ns |   61189 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 73,959,717.1 ns |  987736 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 74,197,140.0 ns |  988901 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Framework 4.6.2 |      9,472.9 ns |    4477 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Framework 4.6.2 |    327,399.8 ns |   70725 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.6.2 |    347,254.4 ns |   71233 B |
