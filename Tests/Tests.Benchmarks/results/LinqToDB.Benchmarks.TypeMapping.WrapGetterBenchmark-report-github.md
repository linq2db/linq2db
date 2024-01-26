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
| TypeMapperString    | .NET 6.0             |  5.3926 ns |         - |
| DirectAccessString  | .NET 6.0             |  1.0806 ns |         - |
| TypeMapperInt       | .NET 6.0             |  5.4801 ns |         - |
| DirectAccessInt     | .NET 6.0             |  0.9119 ns |         - |
| TypeMapperLong      | .NET 6.0             |  5.6439 ns |         - |
| DirectAccessLong    | .NET 6.0             |  0.3457 ns |         - |
| TypeMapperBoolean   | .NET 6.0             |  5.0158 ns |         - |
| DirectAccessBoolean | .NET 6.0             |  0.8643 ns |         - |
| TypeMapperWrapper   | .NET 6.0             | 13.9401 ns |         - |
| DirectAccessWrapper | .NET 6.0             |  0.8581 ns |         - |
| TypeMapperEnum      | .NET 6.0             | 27.3682 ns |      24 B |
| DirectAccessEnum    | .NET 6.0             |  0.9144 ns |         - |
| TypeMapperVersion   | .NET 6.0             |  5.9368 ns |         - |
| DirectAccessVersion | .NET 6.0             |  0.9299 ns |         - |
| TypeMapperString    | .NET 7.0             |  5.2743 ns |         - |
| DirectAccessString  | .NET 7.0             |  0.4320 ns |         - |
| TypeMapperInt       | .NET 7.0             |  5.0541 ns |         - |
| DirectAccessInt     | .NET 7.0             |  0.4559 ns |         - |
| TypeMapperLong      | .NET 7.0             |  5.1791 ns |         - |
| DirectAccessLong    | .NET 7.0             |  0.4611 ns |         - |
| TypeMapperBoolean   | .NET 7.0             |  4.8933 ns |         - |
| DirectAccessBoolean | .NET 7.0             |  0.3743 ns |         - |
| TypeMapperWrapper   | .NET 7.0             | 11.9332 ns |         - |
| DirectAccessWrapper | .NET 7.0             |  0.6135 ns |         - |
| TypeMapperEnum      | .NET 7.0             | 15.1163 ns |         - |
| DirectAccessEnum    | .NET 7.0             |  0.3162 ns |         - |
| TypeMapperVersion   | .NET 7.0             |  5.1355 ns |         - |
| DirectAccessVersion | .NET 7.0             |  0.5462 ns |         - |
| TypeMapperString    | .NET Core 3.1        |  6.0556 ns |         - |
| DirectAccessString  | .NET Core 3.1        |  0.4889 ns |         - |
| TypeMapperInt       | .NET Core 3.1        |  5.2683 ns |         - |
| DirectAccessInt     | .NET Core 3.1        |  1.2241 ns |         - |
| TypeMapperLong      | .NET Core 3.1        |  6.0780 ns |         - |
| DirectAccessLong    | .NET Core 3.1        |  1.1894 ns |         - |
| TypeMapperBoolean   | .NET Core 3.1        |  2.6333 ns |         - |
| DirectAccessBoolean | .NET Core 3.1        |  0.8470 ns |         - |
| TypeMapperWrapper   | .NET Core 3.1        | 12.7370 ns |         - |
| DirectAccessWrapper | .NET Core 3.1        |  1.0184 ns |         - |
| TypeMapperEnum      | .NET Core 3.1        | 33.4762 ns |      24 B |
| DirectAccessEnum    | .NET Core 3.1        |  0.8123 ns |         - |
| TypeMapperVersion   | .NET Core 3.1        |  5.9775 ns |         - |
| DirectAccessVersion | .NET Core 3.1        |  0.4267 ns |         - |
| TypeMapperString    | .NET Framework 4.7.2 | 22.9436 ns |         - |
| DirectAccessString  | .NET Framework 4.7.2 |  0.8870 ns |         - |
| TypeMapperInt       | .NET Framework 4.7.2 | 23.1381 ns |         - |
| DirectAccessInt     | .NET Framework 4.7.2 |  0.8846 ns |         - |
| TypeMapperLong      | .NET Framework 4.7.2 | 23.0376 ns |         - |
| DirectAccessLong    | .NET Framework 4.7.2 |  0.9474 ns |         - |
| TypeMapperBoolean   | .NET Framework 4.7.2 | 23.1942 ns |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.8336 ns |         - |
| TypeMapperWrapper   | .NET Framework 4.7.2 | 34.9260 ns |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  1.4148 ns |         - |
| TypeMapperEnum      | .NET Framework 4.7.2 | 29.5543 ns |      24 B |
| DirectAccessEnum    | .NET Framework 4.7.2 |  1.4384 ns |         - |
| TypeMapperVersion   | .NET Framework 4.7.2 | 21.9195 ns |         - |
| DirectAccessVersion | .NET Framework 4.7.2 |  0.9784 ns |         - |
