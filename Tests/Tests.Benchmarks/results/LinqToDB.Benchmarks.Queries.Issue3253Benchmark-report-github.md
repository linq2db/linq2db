```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5696/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-VZLGGZ : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2
  Job-AZKKUX : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-TQCFWV : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                                                         | Runtime              | Mean            | Allocated |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------:|
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |     89,651.3 ns |   29648 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    103,310.5 ns |   29848 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |     99,452.4 ns |   29648 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |     92,342.4 ns |   29848 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |    223,117.0 ns |   70288 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    234,296.2 ns |   72856 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |    229,864.5 ns |   70288 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |    242,229.6 ns |   72856 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 63,577,386.6 ns | 1001559 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 63,401,323.2 ns | 1002529 B |
| RawAdoNet                                                                      | .NET 6.0             |        169.2 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |     74,635.5 ns |   23456 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |     81,238.7 ns |   24152 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |     77,442.5 ns |   23456 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |     80,765.6 ns |   23736 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |    172,341.4 ns |   55600 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    196,947.4 ns |   55880 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |    184,115.1 ns |   55600 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |    191,601.0 ns |   55880 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 64,003,370.4 ns |  884946 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 69,088,076.0 ns |  885747 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 6.0             |      5,640.2 ns |    4384 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 6.0             |    184,485.3 ns |   53288 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 6.0             |    194,754.4 ns |   53312 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |     45,147.0 ns |   18688 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     50,651.3 ns |   18744 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |     48,355.4 ns |   18560 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |     51,447.7 ns |   19272 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |    120,641.9 ns |   44848 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |    121,729.3 ns |   43928 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |    118,957.9 ns |   43808 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |    113,902.1 ns |   44088 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 60,100,886.7 ns |  952814 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 59,884,490.8 ns |  953174 B |
| RawAdoNet                                                                      | .NET 8.0             |        113.0 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |     39,407.3 ns |   15824 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     38,598.7 ns |   16104 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |     35,433.3 ns |   15824 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |     38,400.7 ns |   16104 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |     96,101.4 ns |   37392 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |    100,501.6 ns |   38152 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |     94,210.0 ns |   37232 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |     94,988.3 ns |   37512 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 66,290,770.5 ns |  844194 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 60,117,126.9 ns |  843098 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 8.0             |      4,009.9 ns |    4384 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 8.0             |    101,186.2 ns |   35080 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 8.0             |    112,543.7 ns |   34816 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    183,953.0 ns |   33117 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    195,819.7 ns |   33621 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    192,929.5 ns |   33116 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    176,841.6 ns |   33621 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    168,023.6 ns |   76551 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    383,032.5 ns |   77057 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    369,307.2 ns |   76553 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    314,022.9 ns |   77051 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 66,730,012.5 ns | 1102918 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 66,599,194.2 ns | 1100814 B |
| RawAdoNet                                                                      | .NET Framework 4.6.2 |        463.0 ns |     417 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    105,824.6 ns |   27062 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    139,388.2 ns |   27568 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    125,693.4 ns |   26286 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |     59,328.3 ns |   26794 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    296,912.8 ns |   64093 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    310,162.6 ns |   64597 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    292,450.8 ns |   64093 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    299,410.1 ns |   64597 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 72,602,196.9 ns |  981889 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 73,220,468.1 ns |  981879 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Framework 4.6.2 |      9,655.7 ns |    4477 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Framework 4.6.2 |    133,742.3 ns |   63495 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.6.2 |    309,343.8 ns |   64401 B |
