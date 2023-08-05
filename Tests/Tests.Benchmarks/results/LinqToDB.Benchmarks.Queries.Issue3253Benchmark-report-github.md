``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.17763.4010/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.201
  [Host]     : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-ZOLDKB : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2
  Job-EHWHZK : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-LWJRKG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-AGOWOF : .NET Framework 4.8 (4.8.4614.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                                                         Method |              Runtime |            Mean | Allocated |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------:|
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    144,675.6 ns |   35552 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    152,609.5 ns |   37272 B |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    138,432.5 ns |   35553 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    138,866.2 ns |   37273 B |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 6.0 |    277,756.6 ns |   84097 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 6.0 |    331,894.2 ns |   85641 B |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 6.0 |    340,645.6 ns |   84097 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 6.0 |    344,024.5 ns |   85641 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 63,704,869.2 ns |  790783 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 63,573,845.8 ns |  795055 B |
|                                                                      RawAdoNet |             .NET 6.0 |        132.9 ns |     360 B |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    105,376.7 ns |   31344 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    126,986.5 ns |   31624 B |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 6.0 |    124,028.6 ns |   31344 B |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    121,467.1 ns |   31976 B |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 6.0 |    204,167.3 ns |   71489 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 6.0 |    298,004.7 ns |   72121 B |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 6.0 |    241,542.9 ns |   71489 B |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 6.0 |    245,871.9 ns |   72121 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 6.0 | 71,303,653.9 ns |  829926 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 6.0 | 69,768,840.6 ns |  830943 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 6.0 |      5,560.7 ns |    4368 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 6.0 |    257,391.3 ns |   68009 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 6.0 |    314,981.0 ns |   69329 B |
|                                 Small_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |     76,851.5 ns |   20976 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |     93,205.4 ns |   21256 B |
|                                   Small_UpdateStatement_With_Static_Parameters |             .NET 7.0 |     88,463.7 ns |   21216 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |     92,079.3 ns |   21256 B |
|                                 Large_UpdateStatement_With_Variable_Parameters |             .NET 7.0 |    220,181.2 ns |   48817 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |             .NET 7.0 |    229,933.8 ns |   49753 B |
|                                   Large_UpdateStatement_With_Static_Parameters |             .NET 7.0 |    222,331.2 ns |   48977 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async |             .NET 7.0 |    219,122.8 ns |   49385 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 68,167,194.7 ns |  766802 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 68,035,844.2 ns |  765978 B |
|                                                                      RawAdoNet |             .NET 7.0 |        145.9 ns |     360 B |
|                                 Small_InsertStatement_With_Variable_Parameters |             .NET 7.0 |     65,493.2 ns |   18720 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |     78,795.5 ns |   19000 B |
|                                   Small_InsertStatement_With_Static_Parameters |             .NET 7.0 |     79,747.4 ns |   18944 B |
|                             Small_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |     81,521.0 ns |   19000 B |
|                                 Large_InsertStatement_With_Variable_Parameters |             .NET 7.0 |    193,539.9 ns |   42465 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async |             .NET 7.0 |    204,835.9 ns |   43225 B |
|                                   Large_InsertStatement_With_Static_Parameters |             .NET 7.0 |    192,123.3 ns |   42529 B |
|                             Large_InsertStatement_With_Static_Parameters_Async |             .NET 7.0 |    198,842.6 ns |   43001 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |             .NET 7.0 | 75,698,300.4 ns |  812098 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |             .NET 7.0 | 75,728,375.2 ns |  811578 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |             .NET 7.0 |      5,328.9 ns |    4368 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |             .NET 7.0 |    189,110.5 ns |   40104 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |             .NET 7.0 |    139,986.3 ns |   40912 B |
|                                 Small_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    188,068.9 ns |   38002 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    200,092.4 ns |   38345 B |
|                                   Small_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    152,703.5 ns |   38000 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    199,034.5 ns |   38345 B |
|                                 Large_UpdateStatement_With_Variable_Parameters |        .NET Core 3.1 |    430,378.8 ns |   90115 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    430,642.5 ns |   87292 B |
|                                   Large_UpdateStatement_With_Static_Parameters |        .NET Core 3.1 |    418,783.1 ns |   90113 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    424,514.8 ns |   87292 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 50,729,591.1 ns |  791480 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 50,396,886.2 ns |  789456 B |
|                                                                      RawAdoNet |        .NET Core 3.1 |        171.0 ns |     360 B |
|                                 Small_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    154,471.0 ns |   32241 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    163,550.9 ns |   31960 B |
|                                   Small_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    157,327.0 ns |   31280 B |
|                             Small_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    145,317.2 ns |   32056 B |
|                                 Large_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |    355,152.5 ns |   71589 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async |        .NET Core 3.1 |    359,128.1 ns |   72361 B |
|                                   Large_InsertStatement_With_Static_Parameters |        .NET Core 3.1 |    314,852.1 ns |   71585 B |
|                             Large_InsertStatement_With_Static_Parameters_Async |        .NET Core 3.1 |    292,722.5 ns |   72361 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches |        .NET Core 3.1 | 55,419,435.9 ns |  819524 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async |        .NET Core 3.1 | 55,601,725.9 ns |  819654 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters |        .NET Core 3.1 |      7,053.1 ns |    4336 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload |        .NET Core 3.1 |    345,925.7 ns |   69113 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async |        .NET Core 3.1 |    328,994.6 ns |   69793 B |
|                                 Small_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    225,185.8 ns |   40197 B |
|                           Small_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    160,296.7 ns |   42233 B |
|                                   Small_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    245,048.7 ns |   40193 B |
|                             Small_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    219,135.6 ns |   42241 B |
|                                 Large_UpdateStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    546,106.9 ns |   95674 B |
|                           Large_UpdateStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    557,339.6 ns |   94233 B |
|                                   Large_UpdateStatement_With_Static_Parameters | .NET Framework 4.7.2 |    547,433.2 ns |   95657 B |
|                             Large_UpdateStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    446,496.3 ns |   94233 B |
|                Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 67,428,563.3 ns |  871441 B |
|          Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 67,653,144.4 ns |  870196 B |
|                                                                      RawAdoNet | .NET Framework 4.7.2 |        472.4 ns |     417 B |
|                                 Small_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    157,266.8 ns |   35748 B |
|                           Small_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    215,817.9 ns |   36248 B |
|                                   Small_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    165,471.4 ns |   33819 B |
|                             Small_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    207,000.9 ns |   34329 B |
|                                 Large_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |    454,843.9 ns |   80165 B |
|                           Large_InsertStatement_With_Variable_Parameters_Async | .NET Framework 4.7.2 |    465,984.4 ns |   80670 B |
|                                   Large_InsertStatement_With_Static_Parameters | .NET Framework 4.7.2 |    456,954.3 ns |   80165 B |
|                             Large_InsertStatement_With_Static_Parameters_Async | .NET Framework 4.7.2 |    468,596.8 ns |   80670 B |
|                Large_InsertStatement_With_Variable_Parameters_With_ClearCaches | .NET Framework 4.7.2 | 75,606,065.7 ns |  910498 B |
|          Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async | .NET Framework 4.7.2 | 75,611,722.9 ns |  911664 B |
|                        Large_Compiled_InsertStatement_With_Variable_Parameters | .NET Framework 4.7.2 |      9,532.3 ns |    4461 B |
|       Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload | .NET Framework 4.7.2 |    434,289.7 ns |   76493 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    223,360.9 ns |   76993 B |
