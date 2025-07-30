```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.17763.7553/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X 3.39GHz, 2 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FTOCRB : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-DHTNJT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-QIENBV : .NET Framework 4.8 (4.8.4795.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                                                         | Runtime              | Mean             | Allocated |
|------------------------------------------------------------------------------- |--------------------- |-----------------:|----------:|
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |     71,145.87 ns |   21280 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     33,955.32 ns |   21640 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |     69,612.50 ns |   21040 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |     65,055.45 ns |   21560 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |    180,386.38 ns |   48976 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |    183,186.48 ns |   50232 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |    169,452.78 ns |   49584 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |    150,523.52 ns |   49528 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 38,615,912.84 ns |  760341 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 50,353,806.67 ns |  760557 B |
| RawAdoNet                                                                      | .NET 8.0             |         91.60 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |     58,790.05 ns |   18656 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     51,869.93 ns |   18936 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |     60,463.73 ns |   18976 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |     61,929.70 ns |   19192 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |    146,631.07 ns |   43040 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |    149,137.62 ns |   43128 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |    153,071.68 ns |   45760 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |    149,320.11 ns |   42680 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 68,380,207.14 ns |  807381 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 53,936,221.43 ns |  808493 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 8.0             |      1,780.36 ns |    4352 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 8.0             |    156,685.40 ns |   39128 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 8.0             |     74,171.33 ns |   39728 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 9.0             |     30,043.62 ns |   21216 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 9.0             |     68,085.31 ns |   21320 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 9.0             |     64,924.61 ns |   20992 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 9.0             |     70,457.77 ns |   21608 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 9.0             |    158,880.66 ns |   49280 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 9.0             |    164,058.95 ns |   49336 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 9.0             |    157,420.07 ns |   48832 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 9.0             |     74,717.23 ns |   49608 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 9.0             | 45,858,617.56 ns |  758629 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 9.0             | 54,089,177.08 ns |  758077 B |
| RawAdoNet                                                                      | .NET 9.0             |        111.46 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 9.0             |     50,216.97 ns |   18768 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 9.0             |     57,877.87 ns |   19096 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 9.0             |     53,660.23 ns |   18608 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 9.0             |     25,341.23 ns |   18936 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 9.0             |    124,457.85 ns |   42192 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 9.0             |    134,129.06 ns |   43064 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 9.0             |    120,795.35 ns |   42720 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 9.0             |    107,934.50 ns |   42776 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 9.0             | 50,290,379.50 ns |  802988 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 9.0             | 26,808,505.29 ns |  803453 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 9.0             |      1,536.97 ns |    4304 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 9.0             |    129,601.72 ns |   39688 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 9.0             |    133,964.26 ns |   39696 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    239,001.96 ns |   40899 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    253,898.06 ns |   42177 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    237,325.50 ns |   40517 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    160,231.76 ns |   42177 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    484,433.71 ns |   92490 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    541,055.47 ns |   94154 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    436,021.68 ns |   92490 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    464,477.11 ns |   94154 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 65,349,000.00 ns |  865293 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 64,927,526.92 ns |  867342 B |
| RawAdoNet                                                                      | .NET Framework 4.6.2 |        393.51 ns |     417 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    178,536.51 ns |   35675 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |     92,096.68 ns |   34643 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    197,105.36 ns |   34136 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    152,119.55 ns |   34639 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    411,730.66 ns |   80478 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    454,908.83 ns |   80982 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    342,791.51 ns |   80478 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    452,784.17 ns |   80982 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 59,243,483.57 ns |  903594 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 54,796,007.80 ns |  905093 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Framework 4.6.2 |      8,464.02 ns |    4493 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Framework 4.6.2 |    421,839.24 ns |   76417 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.6.2 |    483,879.75 ns |   76538 B |
