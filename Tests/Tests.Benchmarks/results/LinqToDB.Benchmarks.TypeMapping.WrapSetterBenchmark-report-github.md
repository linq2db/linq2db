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
| Method              | Runtime              | Mean       | Allocated |
|-------------------- |--------------------- |-----------:|----------:|
| TypeMapperString    | .NET 6.0             |  6.9900 ns |         - |
| DirectAccessString  | .NET 6.0             |  3.6971 ns |         - |
| TypeMapperInt       | .NET 6.0             |  4.6941 ns |         - |
| DirectAccessInt     | .NET 6.0             |  0.9086 ns |         - |
| TypeMapperBoolean   | .NET 6.0             |  5.1498 ns |         - |
| DirectAccessBoolean | .NET 6.0             |  1.1005 ns |         - |
| TypeMapperWrapper   | .NET 6.0             |  2.8593 ns |         - |
| DirectAccessWrapper | .NET 6.0             |  3.7217 ns |         - |
| TypeMapperString    | .NET 7.0             |  8.2979 ns |         - |
| DirectAccessString  | .NET 7.0             |  4.1710 ns |         - |
| TypeMapperInt       | .NET 7.0             |  3.7269 ns |         - |
| DirectAccessInt     | .NET 7.0             |  0.4756 ns |         - |
| TypeMapperBoolean   | .NET 7.0             |  4.6881 ns |         - |
| DirectAccessBoolean | .NET 7.0             |  1.2517 ns |         - |
| TypeMapperWrapper   | .NET 7.0             |  8.7273 ns |         - |
| DirectAccessWrapper | .NET 7.0             |  3.9731 ns |         - |
| TypeMapperString    | .NET Core 3.1        |  8.3471 ns |         - |
| DirectAccessString  | .NET Core 3.1        |  3.5348 ns |         - |
| TypeMapperInt       | .NET Core 3.1        |  6.1283 ns |         - |
| DirectAccessInt     | .NET Core 3.1        |  0.7611 ns |         - |
| TypeMapperBoolean   | .NET Core 3.1        |  2.1581 ns |         - |
| DirectAccessBoolean | .NET Core 3.1        |  0.8508 ns |         - |
| TypeMapperWrapper   | .NET Core 3.1        |  9.3494 ns |         - |
| DirectAccessWrapper | .NET Core 3.1        |  2.9501 ns |         - |
| TypeMapperString    | .NET Framework 4.7.2 | 24.8674 ns |         - |
| DirectAccessString  | .NET Framework 4.7.2 |  4.2201 ns |         - |
| TypeMapperInt       | .NET Framework 4.7.2 | 23.7315 ns |         - |
| DirectAccessInt     | .NET Framework 4.7.2 |  0.6352 ns |         - |
| TypeMapperBoolean   | .NET Framework 4.7.2 | 23.6633 ns |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.4210 ns |         - |
| TypeMapperWrapper   | .NET Framework 4.7.2 | 34.6471 ns |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  4.1107 ns |         - |
