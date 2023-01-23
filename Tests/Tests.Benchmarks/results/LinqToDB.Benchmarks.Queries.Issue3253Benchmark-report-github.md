``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                                                         Method |              Runtime |            Mean | Allocated |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------:|
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    144,699.3 ns |   37376 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    153,263.8 ns |   36248 B |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    149,351.1 ns |   37376 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    149,354.6 ns |   36248 B |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    340,853.3 ns |   84753 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    337,781.3 ns |   85177 B |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    329,754.9 ns |   84753 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    342,490.9 ns |   85177 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 64,198,985.1 ns |  790361 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 63,976,638.3 ns |  792944 B |
|                                                                      RawAdoNet |             .NET 6.0 |        166.8 ns |     360 B |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    125,510.8 ns |   32656 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    124,797.7 ns |   32952 B |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 6.0 |    113,210.2 ns |   32656 B |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    138,367.9 ns |   33288 B |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    290,247.0 ns |   72801 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    302,070.1 ns |   72377 B |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 6.0 |    283,191.0 ns |   72801 B |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    298,917.9 ns |   72377 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 59,923,047.8 ns |  830874 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 44,211,313.7 ns |  831504 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 6.0 |      5,670.1 ns |    4368 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 6.0 |    285,973.3 ns |   68953 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 6.0 |    290,251.5 ns |   70369 B |
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |     90,521.2 ns |   20976 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |     94,872.6 ns |   21256 B |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 7.0 |     71,010.1 ns |   21264 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |     95,764.1 ns |   21512 B |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |    219,702.4 ns |   49089 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |    190,703.6 ns |   49193 B |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 7.0 |    212,571.0 ns |   49009 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |    205,542.4 ns |   49369 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 59,707,364.6 ns |  766587 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 69,351,602.6 ns |  769882 B |
|                                                                      RawAdoNet |             .NET 7.0 |        154.5 ns |     360 B |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 7.0 |     77,139.2 ns |   18944 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |     82,515.9 ns |   19320 B |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 7.0 |     51,539.6 ns |   18720 B |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |     74,920.5 ns |   19000 B |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 7.0 |    188,919.0 ns |   42721 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |    204,653.3 ns |   43449 B |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 7.0 |    131,175.1 ns |   42753 B |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |    202,052.4 ns |   42841 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 67,251,811.9 ns |  811346 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 75,927,561.2 ns |  812122 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 7.0 |      5,428.8 ns |    4368 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 7.0 |    206,282.0 ns |   39352 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 7.0 |    215,909.6 ns |   39472 B |
|                                 Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    198,701.8 ns |   38465 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    187,594.3 ns |   37960 B |
|                                   Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    196,351.5 ns |   38465 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    173,196.8 ns |   37960 B |
|                                 Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    305,765.5 ns |   90577 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    444,597.3 ns |   86907 B |
|                                   Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    417,993.7 ns |   90577 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    402,921.7 ns |   86905 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 50,626,918.0 ns |  787129 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 49,571,956.7 ns |  789824 B |
|                                                                      RawAdoNet |        .NET Core 3.1 |        157.1 ns |     360 B |
|                                 Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    156,226.3 ns |   31537 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    169,325.7 ns |   34040 B |
|                                   Small_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    154,404.7 ns |   33616 B |
|                             Small_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    165,726.5 ns |   34040 B |
|                                 Large_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    342,133.6 ns |   73921 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    289,112.9 ns |   74345 B |
|                                   Large_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    378,127.7 ns |   73923 B |
|                             Large_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    389,787.7 ns |   74345 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 52,086,621.9 ns |  821422 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 57,050,658.5 ns |  820086 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |      8,131.0 ns |    4336 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |        .NET Core 3.1 |    357,115.8 ns |   68441 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |        .NET Core 3.1 |    395,418.7 ns |   70145 B |
|                                 Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    259,248.2 ns |   46369 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    236,019.8 ns |   40311 B |
|                                   Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    225,486.2 ns |   41741 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    254,450.5 ns |   39933 B |
|                                 Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    514,415.7 ns |  101038 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    558,365.1 ns |   92298 B |
|                                   Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    558,965.9 ns |  101042 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    505,445.6 ns |   91909 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 63,062,037.9 ns |  871094 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 66,932,806.2 ns |  877577 B |
|                                                                      RawAdoNet | .NET Framework 4.7.2 |        195.9 ns |     417 B |
|                                 Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    193,522.9 ns |   33823 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    206,257.1 ns |   34327 B |
|                                   Small_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    196,113.1 ns |   33817 B |
|                             Small_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    206,153.3 ns |   34327 B |
|                                 Large_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    445,818.9 ns |   76686 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    453,845.0 ns |   77204 B |
|                                   Large_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    412,775.0 ns |   76686 B |
|                             Large_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    454,847.2 ns |   77204 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 71,642,552.3 ns |  909322 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 68,614,325.4 ns |  907947 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |      8,324.9 ns |    4461 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload | .NET Framework 4.7.2 |    455,977.4 ns |   80330 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    492,351.4 ns |   80857 B |
