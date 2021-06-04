``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |         Mean |       Median |  Ratio | Allocated |
|---------------------- |--------------------- |-------------:|-------------:|-------:|----------:|
|                  Linq |             .NET 5.0 | 138,388.5 ns | 113,516.8 ns | 213.35 |  13,800 B |
|              Compiled |             .NET 5.0 |  43,264.1 ns |  30,134.6 ns |  66.50 |         - |
| FromSql_Interpolation |             .NET 5.0 |  84,101.2 ns |  67,144.6 ns | 128.34 |         - |
|   FromSql_Formattable |             .NET 5.0 |  65,236.0 ns |  57,928.7 ns | 100.50 |         - |
|                 Query |             .NET 5.0 |   1,174.3 ns |   1,161.6 ns |   1.81 |     408 B |
|               Execute |             .NET 5.0 |   1,064.5 ns |   1,063.5 ns |   1.59 |     304 B |
|             RawAdoNet |             .NET 5.0 |     373.2 ns |     368.2 ns |   0.56 |     328 B |
|                  Linq |        .NET Core 3.1 |  55,342.4 ns |  55,757.1 ns |  85.41 |  10,034 B |
|              Compiled |        .NET Core 3.1 |   7,970.2 ns |   8,006.4 ns |  12.25 |   2,672 B |
| FromSql_Interpolation |        .NET Core 3.1 |  99,833.1 ns |  94,207.3 ns | 153.34 |         - |
|   FromSql_Formattable |        .NET Core 3.1 |  94,298.5 ns |  83,674.8 ns | 145.67 |         - |
|                 Query |        .NET Core 3.1 |   1,313.7 ns |   1,315.5 ns |   1.92 |     408 B |
|               Execute |        .NET Core 3.1 |   1,284.8 ns |   1,290.0 ns |   1.96 |     304 B |
|             RawAdoNet |        .NET Core 3.1 |     480.9 ns |     476.7 ns |   0.72 |     328 B |
|                  Linq | .NET Framework 4.7.2 | 160,821.8 ns | 152,136.0 ns | 246.56 |  16,384 B |
|              Compiled | .NET Framework 4.7.2 |  44,775.5 ns |  39,643.1 ns |  69.75 |         - |
| FromSql_Interpolation | .NET Framework 4.7.2 |  31,176.7 ns |  31,301.3 ns |  46.76 |   5,312 B |
|   FromSql_Formattable | .NET Framework 4.7.2 |  33,785.3 ns |  33,597.7 ns |  51.13 |   5,633 B |
|                 Query | .NET Framework 4.7.2 |   1,787.6 ns |   1,794.8 ns |   2.74 |     425 B |
|               Execute | .NET Framework 4.7.2 |   1,649.5 ns |   1,663.8 ns |   2.48 |     321 B |
|             RawAdoNet | .NET Framework 4.7.2 |     651.4 ns |     652.9 ns |   1.00 |     393 B |
