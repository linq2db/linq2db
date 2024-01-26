```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                                                         | Runtime              | Mean            | Allocated |
|------------------------------------------------------------------------------- |--------------------- |----------------:|----------:|
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |    143,849.9 ns |   36256 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    149,775.2 ns |   36552 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |    144,382.8 ns |   36256 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |    149,078.7 ns |   36552 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 6.0             |    317,750.0 ns |   85200 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    290,595.4 ns |   86152 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 6.0             |    338,184.5 ns |   85200 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 6.0             |    348,528.6 ns |   86152 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 62,618,028.3 ns |  787447 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 61,669,649.8 ns |  786162 B |
| RawAdoNet                                                                      | .NET 6.0             |        125.4 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |    124,241.1 ns |   32768 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    126,627.9 ns |   32008 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |    122,191.5 ns |   32768 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |    129,732.6 ns |   32344 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 6.0             |    295,625.2 ns |   73072 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 6.0             |    299,416.8 ns |   72296 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 6.0             |    288,581.2 ns |   73072 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 6.0             |    297,818.0 ns |   72296 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 6.0             | 70,045,887.5 ns |  822996 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 6.0             | 69,855,167.5 ns |  822562 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 6.0             |      5,894.7 ns |    4352 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 6.0             |    299,045.0 ns |   68840 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 6.0             |    314,632.1 ns |   69520 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 7.0             |     92,331.1 ns |   20912 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 7.0             |     95,785.7 ns |   21352 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 7.0             |     89,918.2 ns |   20912 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 7.0             |     93,046.3 ns |   21416 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 7.0             |    221,854.0 ns |   48688 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 7.0             |    234,315.8 ns |   52472 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 7.0             |    220,734.0 ns |   48688 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 7.0             |    224,448.3 ns |   49432 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 7.0             | 66,001,793.3 ns |  761218 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 7.0             | 29,702,822.8 ns |  761509 B |
| RawAdoNet                                                                      | .NET 7.0             |        159.6 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 7.0             |     78,049.9 ns |   18656 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 7.0             |     83,796.0 ns |   18936 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 7.0             |     75,525.5 ns |   18656 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 7.0             |     83,082.6 ns |   19352 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 7.0             |    175,938.2 ns |   42720 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 7.0             |    202,999.7 ns |   42520 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 7.0             |    193,573.3 ns |   42464 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 7.0             |    206,726.4 ns |   42872 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 7.0             | 73,950,548.4 ns |  810594 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 7.0             | 74,077,246.7 ns |  807226 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 7.0             |      5,192.9 ns |    4352 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 7.0             |    166,930.3 ns |   39896 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 7.0             |    214,996.5 ns |   39568 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Core 3.1        |    181,669.7 ns |   36768 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Core 3.1        |    192,880.5 ns |   37289 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Core 3.1        |    183,202.2 ns |   36769 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Core 3.1        |    195,186.4 ns |   37289 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Core 3.1        |    424,985.6 ns |   85716 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Core 3.1        |    430,085.2 ns |   86232 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Core 3.1        |    385,156.5 ns |   85712 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Core 3.1        |    445,982.3 ns |   86232 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Core 3.1        | 49,833,864.0 ns |  779752 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Core 3.1        | 49,471,800.6 ns |  780539 B |
| RawAdoNet                                                                      | .NET Core 3.1        |        161.2 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Core 3.1        |    106,976.4 ns |   32144 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Core 3.1        |    166,975.2 ns |   32571 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Core 3.1        |    154,565.4 ns |   32145 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Core 3.1        |    157,959.3 ns |   32568 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Core 3.1        |    324,515.2 ns |   72448 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Core 3.1        |    371,923.4 ns |   72872 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Core 3.1        |    371,453.5 ns |   72448 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Core 3.1        |    367,459.0 ns |   72872 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Core 3.1        | 55,428,099.2 ns |  814798 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Core 3.1        | 55,234,228.3 ns |  815242 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Core 3.1        |      8,068.1 ns |    4320 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Core 3.1        |    347,646.5 ns |   69656 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Core 3.1        |    395,729.5 ns |   70432 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.7.2 |    242,354.5 ns |   39365 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.7.2 |    113,577.6 ns |   40248 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.7.2 |    219,283.8 ns |   39363 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.7.2 |    258,979.1 ns |   40253 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.7.2 |    549,486.3 ns |   91345 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.7.2 |    564,849.5 ns |   92226 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.7.2 |    543,403.6 ns |   91345 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.7.2 |    512,530.6 ns |   92226 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.7.2 | 64,908,989.2 ns |  865287 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.7.2 | 65,902,788.3 ns |  866314 B |
| RawAdoNet                                                                      | .NET Framework 4.7.2 |        457.6 ns |     417 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.7.2 |    202,210.4 ns |   35679 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.7.2 |    207,934.5 ns |   37343 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.7.2 |    203,350.8 ns |   35297 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.7.2 |    201,830.1 ns |   34261 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.7.2 |    461,031.9 ns |   81634 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.7.2 |    467,929.7 ns |   80589 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.7.2 |    453,709.1 ns |   81634 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.7.2 |    457,925.7 ns |   80589 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.7.2 | 48,269,339.3 ns |  905640 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.7.2 | 72,795,879.6 ns |  905832 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Framework 4.7.2 |      9,765.8 ns |    4445 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Framework 4.7.2 |    482,429.4 ns |   77569 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.7.2 |    491,991.7 ns |   76921 B |
