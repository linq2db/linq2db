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
|              Method |              Runtime |      Mean |    Median | Ratio | Allocated |
|-------------------- |--------------------- |----------:|----------:|------:|----------:|
|    TypeMapperString |             .NET 5.0 |  8.333 ns |  8.334 ns |  2.75 |         - |
|  DirectAccessString |             .NET 5.0 |  3.347 ns |  3.260 ns |  1.12 |         - |
|       TypeMapperInt |             .NET 5.0 |  6.283 ns |  6.208 ns |  2.09 |         - |
|     DirectAccessInt |             .NET 5.0 |  1.138 ns |  1.084 ns |  0.38 |         - |
|   TypeMapperBoolean |             .NET 5.0 |  6.374 ns |  6.347 ns |  2.09 |         - |
| DirectAccessBoolean |             .NET 5.0 |  1.029 ns |  1.012 ns |  0.34 |         - |
|   TypeMapperWrapper |             .NET 5.0 |  9.243 ns |  9.179 ns |  3.03 |         - |
| DirectAccessWrapper |             .NET 5.0 |  3.108 ns |  3.019 ns |  1.05 |         - |
|    TypeMapperString |        .NET Core 3.1 |  7.933 ns |  7.810 ns |  2.64 |         - |
|  DirectAccessString |        .NET Core 3.1 |  2.860 ns |  2.793 ns |  0.94 |         - |
|       TypeMapperInt |        .NET Core 3.1 |  6.081 ns |  6.132 ns |  2.01 |         - |
|     DirectAccessInt |        .NET Core 3.1 |  1.273 ns |  1.274 ns |  0.42 |         - |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.075 ns |  6.101 ns |  2.03 |         - |
| DirectAccessBoolean |        .NET Core 3.1 |  1.227 ns |  1.226 ns |  0.41 |         - |
|   TypeMapperWrapper |        .NET Core 3.1 |  9.133 ns |  8.884 ns |  3.00 |         - |
| DirectAccessWrapper |        .NET Core 3.1 |  2.695 ns |  2.692 ns |  0.89 |         - |
|    TypeMapperString | .NET Framework 4.7.2 | 22.587 ns | 22.221 ns |  7.33 |         - |
|  DirectAccessString | .NET Framework 4.7.2 |  3.057 ns |  3.001 ns |  1.00 |         - |
|       TypeMapperInt | .NET Framework 4.7.2 | 20.551 ns | 20.288 ns |  6.70 |         - |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.098 ns |  1.113 ns |  0.36 |         - |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 23.167 ns | 22.911 ns |  7.63 |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.118 ns |  1.104 ns |  0.37 |         - |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 29.870 ns | 29.779 ns | 10.08 |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  2.976 ns |  2.982 ns |  0.98 |         - |
