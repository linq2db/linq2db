```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.5328/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-KJWIMT : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GULBRG : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-LRGNRQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-SJROSW : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                                                         | Runtime              | Mean            | Allocated |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------:|
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |    146,971.8 ns |   37344 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    155,993.7 ns |   37672 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |    137,077.4 ns |   37344 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |    159,088.0 ns |   37672 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |    153,034.2 ns |   86288 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    342,093.6 ns |   85912 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |    345,604.0 ns |   86288 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |    316,918.4 ns |   85912 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 63,259,482.5 ns |  787032 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 63,674,476.7 ns |  791812 B |
| RawAdoNet                                                                      | .NET 6.0             |        173.8 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |    126,745.9 ns |   31888 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    121,713.4 ns |   31816 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |    122,422.3 ns |   31536 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |    126,125.1 ns |   31816 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |    134,553.6 ns |   71840 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    304,536.1 ns |   72120 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |    294,185.9 ns |   71840 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |    298,645.1 ns |   72120 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 64,536,765.3 ns |  823130 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 69,550,005.0 ns |  823039 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 6.0             |      6,027.2 ns |    4352 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 6.0             |    312,570.9 ns |   68376 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 6.0             |    319,981.3 ns |   69680 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 7.0             |     82,752.8 ns |   21200 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 7.0             |     94,530.0 ns |   21192 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 7.0             |     87,182.5 ns |   20912 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 7.0             |     96,247.1 ns |   21672 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 7.0             |    220,617.6 ns |   48992 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 7.0             |    233,009.9 ns |   48968 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 7.0             |    221,549.0 ns |   48768 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 7.0             |    103,294.1 ns |   49192 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 7.0             | 66,183,654.2 ns |  764498 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 7.0             | 66,697,713.3 ns |  764314 B |
| RawAdoNet                                                                      | .NET 7.0             |        146.4 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 7.0             |     80,373.5 ns |   18880 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 7.0             |     80,577.8 ns |   18936 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 7.0             |     80,319.7 ns |   18656 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 7.0             |     84,332.0 ns |   18936 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 7.0             |    196,497.4 ns |   42240 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 7.0             |    204,784.3 ns |   42520 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 7.0             |    198,550.6 ns |   42240 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 7.0             |    201,456.6 ns |   42520 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 7.0             | 73,797,663.2 ns |  807346 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 7.0             | 68,700,283.2 ns |  806878 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 7.0             |      5,113.1 ns |    4352 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 7.0             |     92,083.7 ns |   39992 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 7.0             |    213,710.5 ns |   39792 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Core 3.1        |    184,778.0 ns |   37568 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Core 3.1        |    202,664.5 ns |   39304 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Core 3.1        |    184,287.6 ns |   37570 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Core 3.1        |    196,271.1 ns |   39305 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Core 3.1        |    427,295.4 ns |   86517 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Core 3.1        |    405,154.8 ns |   94584 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Core 3.1        |    424,630.5 ns |   86514 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Core 3.1        |    456,115.5 ns |   94584 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Core 3.1        | 22,739,843.4 ns |  782716 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Core 3.1        | 50,167,795.3 ns |  780746 B |
| RawAdoNet                                                                      | .NET Core 3.1        |        142.9 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Core 3.1        |    153,792.0 ns |   32497 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Core 3.1        |    156,092.2 ns |   32568 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Core 3.1        |    113,058.3 ns |   32496 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Core 3.1        |    146,346.1 ns |   32568 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Core 3.1        |    360,451.5 ns |   72800 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Core 3.1        |    365,838.6 ns |   72877 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Core 3.1        |    357,666.2 ns |   72802 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Core 3.1        |    370,392.4 ns |   72872 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Core 3.1        | 54,687,823.1 ns |  817706 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Core 3.1        | 57,043,955.0 ns |  818932 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Core 3.1        |      8,711.2 ns |    4320 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Core 3.1        |    372,226.1 ns |   69304 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Core 3.1        |    379,996.9 ns |   69731 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.7.2 |    233,072.3 ns |   39749 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.7.2 |    250,248.3 ns |   40641 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.7.2 |    107,486.7 ns |   39749 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.7.2 |    254,084.0 ns |   40641 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.7.2 |    531,743.4 ns |   91730 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.7.2 |    549,585.0 ns |   92627 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.7.2 |    541,371.6 ns |   91730 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.7.2 |    550,632.2 ns |   92614 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.7.2 | 65,571,932.5 ns |  865293 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.7.2 | 59,335,734.1 ns |  865630 B |
| RawAdoNet                                                                      | .NET Framework 4.7.2 |        459.7 ns |     417 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.7.2 |    191,918.1 ns |   34135 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.7.2 |    208,501.3 ns |   35031 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.7.2 |    192,138.5 ns |   33753 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.7.2 |    201,820.9 ns |   34261 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.7.2 |    456,047.0 ns |   80089 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.7.2 |    461,825.8 ns |   80586 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.7.2 |    446,717.4 ns |   80089 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.7.2 |    464,091.6 ns |   80589 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.7.2 | 73,385,860.0 ns |  904943 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.7.2 | 72,128,753.3 ns |  905821 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Framework 4.7.2 |      9,132.7 ns |    4445 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Framework 4.7.2 |    473,241.8 ns |   75657 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    466,872.5 ns |   76550 B |
