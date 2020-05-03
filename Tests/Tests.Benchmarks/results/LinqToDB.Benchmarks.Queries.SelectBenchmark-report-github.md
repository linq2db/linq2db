``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-OGAWJV : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZLSLVN : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                Method |       Runtime |         Mean |       Median |    Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |-------------- |-------------:|-------------:|---------:|-------:|------:|------:|----------:|
|                  Linq |    .NET 4.6.2 | 213,476.0 ns | 170,567.8 ns | 1,066.09 |      - |     - |     - |   16384 B |
|              Compiled |    .NET 4.6.2 |  45,068.7 ns |  38,326.6 ns |   218.48 |      - |     - |     - |         - |
| FromSql_Interpolation |    .NET 4.6.2 | 144,864.4 ns | 122,879.1 ns |   679.97 |      - |     - |     - |         - |
|   FromSql_Formattable |    .NET 4.6.2 |  96,057.2 ns |  79,286.3 ns |   507.39 |      - |     - |     - |         - |
|                 Query |    .NET 4.6.2 |     609.5 ns |     597.7 ns |     3.23 | 0.1011 |     - |     - |     425 B |
|               Execute |    .NET 4.6.2 |     512.6 ns |     512.8 ns |     2.70 | 0.0763 |     - |     - |     321 B |
|             RawAdoNet |    .NET 4.6.2 |     189.6 ns |     189.3 ns |     1.00 | 0.0439 |     - |     - |     185 B |
|                  Linq | .NET Core 2.1 | 162,525.2 ns | 124,488.2 ns | 1,018.05 |      - |     - |     - |    8616 B |
|              Compiled | .NET Core 2.1 |  28,980.2 ns |  22,235.3 ns |   229.00 |      - |     - |     - |         - |
| FromSql_Interpolation | .NET Core 2.1 |  76,414.6 ns |  53,247.6 ns |   315.59 |      - |     - |     - |         - |
|   FromSql_Formattable | .NET Core 2.1 |  87,761.6 ns |  71,533.2 ns |   496.18 |      - |     - |     - |         - |
|                 Query | .NET Core 2.1 |     490.9 ns |     489.4 ns |     2.60 | 0.0963 |     - |     - |     408 B |
|               Execute | .NET Core 2.1 |     449.9 ns |     445.3 ns |     2.34 | 0.0725 |     - |     - |     304 B |
|             RawAdoNet | .NET Core 2.1 |     138.1 ns |     135.6 ns |     0.73 | 0.0379 |     - |     - |     160 B |
|                  Linq | .NET Core 3.1 | 234,824.9 ns | 181,100.3 ns | 1,480.60 |      - |     - |     - |    8520 B |
|              Compiled | .NET Core 3.1 |  54,804.9 ns |  45,055.7 ns |   313.64 |      - |     - |     - |         - |
| FromSql_Interpolation | .NET Core 3.1 |  21,260.7 ns |  20,839.1 ns |   111.11 | 1.3428 |     - |     - |    5696 B |
|   FromSql_Formattable | .NET Core 3.1 | 124,917.6 ns |  90,696.5 ns |   813.86 |      - |     - |     - |         - |
|                 Query | .NET Core 3.1 |     530.5 ns |     515.1 ns |     2.66 | 0.0973 |     - |     - |     408 B |
|               Execute | .NET Core 3.1 |     423.9 ns |     422.9 ns |     2.25 | 0.0725 |     - |     - |     304 B |
|             RawAdoNet | .NET Core 3.1 |     130.8 ns |     127.0 ns |     0.73 | 0.0381 |     - |     - |     160 B |
