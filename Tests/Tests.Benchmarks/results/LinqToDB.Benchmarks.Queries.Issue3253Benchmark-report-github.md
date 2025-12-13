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
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |     81,583.57 ns |   30256 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     88,670.45 ns |   30616 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |     81,761.39 ns |   30256 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |     61,314.99 ns |   30616 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 8.0             |    142,207.85 ns |   61168 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 8.0             |    178,508.20 ns |   61272 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 8.0             |    146,570.07 ns |   61408 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 8.0             |    176,931.03 ns |   61432 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 64,635,508.33 ns |  839995 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 64,986,936.26 ns |  838435 B |
| RawAdoNet                                                                      | .NET 8.0             |         67.06 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |     33,910.98 ns |   22912 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |     52,421.48 ns |   23208 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |     48,643.35 ns |   22688 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |     52,327.21 ns |   23400 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 8.0             |    116,318.30 ns |   49168 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 8.0             |    104,590.91 ns |   49912 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 8.0             |    118,614.10 ns |   49168 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 8.0             |    122,789.90 ns |   49912 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 8.0             | 48,215,312.38 ns |  725312 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 8.0             | 63,804,046.03 ns |  725171 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 8.0             |     10,398.32 ns |   11440 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 8.0             |    103,488.85 ns |   34504 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 8.0             |     89,211.07 ns |   34880 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET 9.0             |     88,900.80 ns |   30048 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET 9.0             |     42,893.58 ns |   30680 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET 9.0             |     86,923.02 ns |   30048 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET 9.0             |     92,878.96 ns |   30456 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET 9.0             |    189,884.28 ns |   61264 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET 9.0             |    198,077.73 ns |   61928 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET 9.0             |    197,567.88 ns |   61488 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET 9.0             |    199,954.78 ns |   61560 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET 9.0             | 47,216,225.88 ns |  831056 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 9.0             | 56,873,041.67 ns |  831304 B |
| RawAdoNet                                                                      | .NET 9.0             |         98.85 ns |     360 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET 9.0             |     45,780.22 ns |   22640 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET 9.0             |     38,832.59 ns |   23048 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET 9.0             |     47,051.11 ns |   22640 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET 9.0             |     48,394.90 ns |   23048 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET 9.0             |    103,300.16 ns |   49120 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET 9.0             |     49,030.58 ns |   50008 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET 9.0             |    105,582.49 ns |   49440 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET 9.0             |    106,575.78 ns |   49528 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET 9.0             | 55,339,359.26 ns |  717959 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET 9.0             | 54,538,514.77 ns |  716952 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET 9.0             |     11,509.63 ns |   11392 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET 9.0             |     58,092.90 ns |   34712 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET 9.0             |     92,650.28 ns |   34832 B |
| Small_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    193,756.46 ns |   44445 B |
| Small_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    276,733.93 ns |   44665 B |
| Small_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    267,512.95 ns |   44445 B |
| Small_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    250,117.73 ns |   44665 B |
| Large_UpdateStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    414,348.31 ns |   96586 B |
| Large_UpdateStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    386,395.46 ns |   93338 B |
| Large_UpdateStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    500,714.20 ns |   96594 B |
| Large_UpdateStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    318,349.56 ns |   93338 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 52,943,763.31 ns | 1152528 B |
| Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 70,418,201.43 ns | 1154499 B |
| RawAdoNet                                                                      | .NET Framework 4.6.2 |        388.20 ns |     417 B |
| Small_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    165,595.82 ns |   33593 B |
| Small_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    176,925.53 ns |   33805 B |
| Small_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    136,605.39 ns |   33973 B |
| Small_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    144,129.19 ns |   34578 B |
| Large_InsertStatement_With_Variable_Parameters                                 | .NET Framework 4.6.2 |    272,128.20 ns |   73394 B |
| Large_InsertStatement_With_Variable_Parameters_Async                           | .NET Framework 4.6.2 |    357,442.87 ns |   74006 B |
| Large_InsertStatement_With_Static_Parameters                                   | .NET Framework 4.6.2 |    155,962.81 ns |   73394 B |
| Large_InsertStatement_With_Static_Parameters_Async                             | .NET Framework 4.6.2 |    304,829.26 ns |   74006 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches                | .NET Framework 4.6.2 | 47,964,124.44 ns |  991244 B |
| Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async          | .NET Framework 4.6.2 | 67,549,536.67 ns |  990221 B |
| Large_Compiled_InsertStatement_With_Variable_Parameters                        | .NET Framework 4.6.2 |     21,678.21 ns |   11586 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload       | .NET Framework 4.6.2 |    132,728.29 ns |   59485 B |
| Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async | .NET Framework 4.6.2 |    321,644.88 ns |   64713 B |
