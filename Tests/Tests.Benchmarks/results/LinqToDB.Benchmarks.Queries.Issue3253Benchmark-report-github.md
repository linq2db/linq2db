```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                                                         | Runtime              | Mean             | Allocated |
|------------------------------------------------------------------------------- |--------------------- |-----------------:|----------:|
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |    169,922.56 ns |   42224 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    181,519.28 ns |   43704 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |    175,824.64 ns |   42224 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |    157,875.88 ns |   43512 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |    321,732.05 ns |   91120 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    334,496.85 ns |   94968 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |    300,109.31 ns |   91120 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |    338,535.62 ns |   94968 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 61,273,693.00 ns |  950125 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 65,802,467.86 ns |  954271 B |
| RawAdoNet                                                                      | .NET 6.0             |        173.95 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |     96,742.56 ns |   30528 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    101,058.26 ns |   30760 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |     93,873.32 ns |   29856 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |     98,094.27 ns |   30072 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |    214,218.85 ns |   66800 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    224,598.50 ns |   67720 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |    218,233.45 ns |   66800 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |    231,411.70 ns |   67720 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 63,903,205.83 ns |  825320 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 65,021,096.67 ns |  825526 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 6.0             |     18,447.24 ns |   10992 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 6.0             |    188,721.37 ns |   52665 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 6.0             |    203,997.77 ns |   53377 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |     92,243.32 ns |   31408 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     96,371.57 ns |   31560 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |     91,958.82 ns |   31840 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |     43,070.42 ns |   32104 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |    177,470.38 ns |   61504 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     84,611.38 ns |   61784 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |    181,444.75 ns |   61568 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |    187,644.76 ns |   61720 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 28,681,758.24 ns |  830859 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 64,063,690.48 ns |  835107 B |
| RawAdoNet                                                                      | .NET 8.0             |        115.52 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |     47,845.39 ns |   22224 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     51,707.16 ns |   22664 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |     49,177.80 ns |   22768 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |     40,098.88 ns |   22920 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |    116,835.71 ns |   48272 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |    115,735.75 ns |   49000 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |    111,932.09 ns |   48432 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |    118,848.51 ns |   48648 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 63,008,798.33 ns |  718838 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 63,118,326.47 ns |  719550 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 8.0             |     11,706.20 ns |   10992 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 8.0             |     99,804.98 ns |   34328 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 8.0             |     96,654.22 ns |   37521 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 9.0             |     75,765.41 ns |   31424 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 9.0             |     85,607.33 ns |   31561 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 9.0             |     77,348.24 ns |   31073 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 9.0             |     84,462.73 ns |   31337 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 9.0             |    160,148.71 ns |   61745 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 9.0             |    153,257.28 ns |   62297 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 9.0             |    147,075.82 ns |   61297 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 9.0             |    165,757.44 ns |   61785 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 9.0             | 55,440,097.50 ns |  822442 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 9.0             | 55,090,403.13 ns |  825346 B |
| RawAdoNet                                                                      | .NET 9.0             |         99.50 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 9.0             |     43,513.88 ns |   22176 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 9.0             |     46,512.78 ns |   22856 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 9.0             |     44,558.97 ns |   22688 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 9.0             |     46,289.18 ns |   22600 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 9.0             |    100,018.86 ns |   48385 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 9.0             |    105,552.55 ns |   48937 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 9.0             |     91,885.57 ns |   48481 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 9.0             |    107,493.06 ns |   48809 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 9.0             | 54,646,749.63 ns |  708032 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 9.0             | 54,452,715.00 ns |  711314 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 9.0             |     12,142.61 ns |   10944 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 9.0             |     89,819.53 ns |   34952 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 9.0             |     94,404.89 ns |   34833 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    285,057.09 ns |   48433 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    268,064.68 ns |   47337 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    284,928.55 ns |   48433 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    293,411.76 ns |   47333 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    512,906.74 ns |  117483 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    534,427.94 ns |   99034 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    548,834.16 ns |  117483 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    531,936.47 ns |   99034 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 70,987,938.33 ns | 1079304 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 65,307,603.25 ns | 1083405 B |
| RawAdoNet                                                                      | .NET Framework 4.6.2 |        431.34 ns |     417 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    147,372.89 ns |   33143 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    172,103.23 ns |   33969 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    157,004.93 ns |   33142 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    167,838.72 ns |   33204 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    344,654.05 ns |   75598 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    325,830.66 ns |   75661 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    344,884.78 ns |   75598 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    352,513.67 ns |   75661 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 63,047,215.44 ns |  939369 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 68,620,476.67 ns |  939022 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Framework 4.6.2 |     25,788.80 ns |   11153 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Framework 4.6.2 |    303,251.84 ns |   59477 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.6.2 |    302,395.03 ns |   60301 B |
