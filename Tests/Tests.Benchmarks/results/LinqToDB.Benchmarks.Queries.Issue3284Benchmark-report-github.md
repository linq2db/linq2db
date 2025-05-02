``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-KUESZS : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ANAWII : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-ULZYVB : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|            Method |              Runtime |        Mean |    Median | Ratio | Allocated |
|------------------ |--------------------- |------------:|----------:|------:|----------:|
|          Compiled |             .NET 5.0 |    89.14 μs |  89.04 μs |  0.11 |     48 KB |
|            String |             .NET 5.0 |   291.67 μs | 287.57 μs |  0.39 |     88 KB |
|   String_Nullable |             .NET 5.0 |   298.11 μs | 289.32 μs |  0.41 |     89 KB |
|               Int |             .NET 5.0 |   215.57 μs | 214.36 μs |  0.30 |     63 KB |
|      Int_Nullable |             .NET 5.0 |   209.00 μs | 208.32 μs |  0.29 |     64 KB |
|          DateTime |             .NET 5.0 |   210.54 μs | 210.95 μs |  0.27 |     63 KB |
| DateTime_Nullable |             .NET 5.0 |   226.19 μs | 223.82 μs |  0.31 |     64 KB |
|              Bool |             .NET 5.0 |   210.74 μs | 210.18 μs |  0.29 |     63 KB |
|     Bool_Nullable |             .NET 5.0 |   217.05 μs | 215.50 μs |  0.25 |     64 KB |
|           Decimal |             .NET 5.0 |   214.59 μs | 214.90 μs |  0.28 |     63 KB |
|  Decimal_Nullable |             .NET 5.0 |   221.25 μs | 219.51 μs |  0.26 |     64 KB |
|             Float |             .NET 5.0 |   222.10 μs | 221.56 μs |  0.29 |     65 KB |
|    Float_Nullable |             .NET 5.0 |   232.69 μs | 230.07 μs |  0.29 |     64 KB |
|          Compiled |        .NET Core 3.1 |   158.62 μs | 156.99 μs |  0.22 |     49 KB |
|            String |        .NET Core 3.1 |   394.61 μs | 388.42 μs |  0.53 |     90 KB |
|   String_Nullable |        .NET Core 3.1 |   393.82 μs | 392.02 μs |  0.48 |     89 KB |
|               Int |        .NET Core 3.1 |   327.43 μs | 321.71 μs |  0.45 |     65 KB |
|      Int_Nullable |        .NET Core 3.1 |   324.59 μs | 321.28 μs |  0.40 |     78 KB |
|          DateTime |        .NET Core 3.1 |   331.96 μs | 325.46 μs |  0.45 |     65 KB |
| DateTime_Nullable |        .NET Core 3.1 |   363.60 μs | 356.29 μs |  0.50 |     78 KB |
|              Bool |        .NET Core 3.1 |   292.96 μs | 295.65 μs |  0.35 |     64 KB |
|     Bool_Nullable |        .NET Core 3.1 |   339.02 μs | 334.89 μs |  0.46 |     77 KB |
|           Decimal |        .NET Core 3.1 |   336.54 μs | 328.60 μs |  0.47 |     65 KB |
|  Decimal_Nullable |        .NET Core 3.1 |   341.59 μs | 342.84 μs |  0.44 |     78 KB |
|             Float |        .NET Core 3.1 |   307.26 μs | 303.33 μs |  0.42 |     64 KB |
|    Float_Nullable |        .NET Core 3.1 |   372.11 μs | 370.75 μs |  0.47 |     78 KB |
|          Compiled | .NET Framework 4.7.2 |   813.67 μs | 683.73 μs |  1.00 |    112 KB |
|            String | .NET Framework 4.7.2 | 1,040.94 μs | 909.31 μs |  1.45 |    128 KB |
|   String_Nullable | .NET Framework 4.7.2 | 1,114.85 μs | 995.47 μs |  1.55 |    128 KB |
|               Int | .NET Framework 4.7.2 |   941.83 μs | 800.76 μs |  1.33 |     96 KB |
|      Int_Nullable | .NET Framework 4.7.2 |   972.08 μs | 874.78 μs |  1.35 |     96 KB |
|          DateTime | .NET Framework 4.7.2 | 1,022.94 μs | 965.19 μs |  1.41 |    104 KB |
| DateTime_Nullable | .NET Framework 4.7.2 | 1,001.07 μs | 926.57 μs |  1.38 |     96 KB |
|              Bool | .NET Framework 4.7.2 | 1,042.38 μs | 996.20 μs |  1.42 |     96 KB |
|     Bool_Nullable | .NET Framework 4.7.2 | 1,075.24 μs | 912.67 μs |  1.51 |     96 KB |
|           Decimal | .NET Framework 4.7.2 |   923.84 μs | 832.65 μs |  1.27 |    104 KB |
|  Decimal_Nullable | .NET Framework 4.7.2 |   902.08 μs | 818.61 μs |  1.24 |     96 KB |
|             Float | .NET Framework 4.7.2 |   957.79 μs | 855.18 μs |  1.34 |    104 KB |
|    Float_Nullable | .NET Framework 4.7.2 |   883.77 μs | 847.57 μs |  1.21 |     96 KB |
