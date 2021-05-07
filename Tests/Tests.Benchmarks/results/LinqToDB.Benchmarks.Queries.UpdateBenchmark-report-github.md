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
|            LinqSet |             .NET 5.0 | 231,587.1 ns | 230,480.0 ns | 395.38 |  61,116 B |
|         LinqObject |             .NET 5.0 | 136,719.5 ns | 137,065.8 ns | 234.26 |  31,709 B |
|             Object |             .NET 5.0 |  15,975.8 ns |  16,038.6 ns |  27.30 |   8,144 B |
|    CompiledLinqSet |             .NET 5.0 |  63,993.1 ns |  57,197.3 ns | 106.48 |         - |
| CompiledLinqObject |             .NET 5.0 |  70,366.0 ns |  64,218.9 ns | 108.58 |         - |
|          RawAdoNet |             .NET 5.0 |     272.8 ns |     273.5 ns |   0.47 |     712 B |
|            LinqSet |        .NET Core 3.1 | 274,151.4 ns | 279,201.1 ns | 466.57 |  62,669 B |
|         LinqObject |        .NET Core 3.1 | 158,693.0 ns | 155,724.6 ns | 269.86 |  39,711 B |
|             Object |        .NET Core 3.1 |  72,224.1 ns |  61,732.1 ns | 121.69 |   8,288 B |
|    CompiledLinqSet |        .NET Core 3.1 |  16,190.6 ns |  15,864.9 ns |  27.57 |   7,792 B |
| CompiledLinqObject |        .NET Core 3.1 |  16,582.0 ns |  16,266.6 ns |  27.93 |   7,792 B |
|          RawAdoNet |        .NET Core 3.1 |     254.5 ns |     254.0 ns |   0.44 |     712 B |
|            LinqSet | .NET Framework 4.7.2 | 508,303.2 ns | 508,485.2 ns | 879.29 |  81,920 B |
|         LinqObject | .NET Framework 4.7.2 | 347,457.0 ns | 332,797.4 ns | 592.90 |  49,152 B |
|             Object | .NET Framework 4.7.2 |  23,797.9 ns |  23,752.5 ns |  40.16 |   9,452 B |
|    CompiledLinqSet | .NET Framework 4.7.2 |  48,964.9 ns |  43,739.1 ns |  84.05 |         - |
| CompiledLinqObject | .NET Framework 4.7.2 |  50,589.3 ns |  48,859.1 ns |  90.21 |         - |
|          RawAdoNet | .NET Framework 4.7.2 |     591.4 ns |     587.0 ns |   1.00 |     810 B |
