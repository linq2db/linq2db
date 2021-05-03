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
|             Method |              Runtime |         Mean |       Median |  Ratio | Allocated |
|------------------- |--------------------- |-------------:|-------------:|-------:|----------:|
|            LinqSet |             .NET 5.0 | 572,856.6 ns | 511,996.1 ns | 960.81 |  61,432 B |
|         LinqObject |             .NET 5.0 | 343,925.6 ns | 331,773.5 ns | 695.21 |  33,216 B |
|             Object |             .NET 5.0 |  13,376.0 ns |  13,293.6 ns |  25.35 |   8,256 B |
|    CompiledLinqSet |             .NET 5.0 |  59,791.9 ns |  49,444.2 ns | 137.89 |         - |
| CompiledLinqObject |             .NET 5.0 |  58,185.5 ns |  53,979.0 ns | 112.04 |         - |
|          RawAdoNet |             .NET 5.0 |     261.0 ns |     251.9 ns |   0.48 |     712 B |
|            LinqSet |        .NET Core 3.1 | 243,909.3 ns | 241,613.3 ns | 453.97 |  61,485 B |
|         LinqObject |        .NET Core 3.1 | 141,441.2 ns | 140,322.5 ns | 266.36 |  39,167 B |
|             Object |        .NET Core 3.1 |  16,221.0 ns |  15,989.1 ns |  30.76 |   8,224 B |
|    CompiledLinqSet |        .NET Core 3.1 |  69,670.0 ns |  57,928.7 ns | 131.98 |         - |
| CompiledLinqObject |        .NET Core 3.1 |  74,914.0 ns |  67,583.5 ns | 132.30 |         - |
|          RawAdoNet |        .NET Core 3.1 |     244.2 ns |     244.5 ns |   0.47 |     712 B |
|            LinqSet | .NET Framework 4.7.2 | 316,086.2 ns | 314,072.2 ns | 608.27 |  70,846 B |
|         LinqObject | .NET Framework 4.7.2 | 162,389.3 ns | 161,472.0 ns | 306.22 |  40,733 B |
|             Object | .NET Framework 4.7.2 |  20,219.3 ns |  20,244.6 ns |  37.99 |   9,404 B |
|    CompiledLinqSet | .NET Framework 4.7.2 |  15,634.8 ns |  15,590.8 ns |  29.40 |   8,200 B |
| CompiledLinqObject | .NET Framework 4.7.2 |  17,258.2 ns |  17,261.9 ns |  32.43 |   9,067 B |
|          RawAdoNet | .NET Framework 4.7.2 |     532.1 ns |     533.1 ns |   1.00 |     810 B |
