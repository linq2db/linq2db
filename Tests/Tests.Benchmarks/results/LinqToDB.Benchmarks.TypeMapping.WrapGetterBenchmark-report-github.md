```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method              | Runtime              | Mean       | Allocated |
|-------------------- |--------------------- |-----------:|----------:|
| TypeMapperString    | .NET 6.0             |  5.3313 ns |         - |
| DirectAccessString  | .NET 6.0             |  0.8551 ns |         - |
| TypeMapperInt       | .NET 6.0             |  5.5689 ns |         - |
| DirectAccessInt     | .NET 6.0             |  0.9420 ns |         - |
| TypeMapperLong      | .NET 6.0             |  5.5686 ns |         - |
| DirectAccessLong    | .NET 6.0             |  0.9191 ns |         - |
| TypeMapperBoolean   | .NET 6.0             |  4.7704 ns |         - |
| DirectAccessBoolean | .NET 6.0             |  0.4865 ns |         - |
| TypeMapperWrapper   | .NET 6.0             | 12.8420 ns |         - |
| DirectAccessWrapper | .NET 6.0             |  1.0687 ns |         - |
| TypeMapperEnum      | .NET 6.0             | 28.3631 ns |      24 B |
| DirectAccessEnum    | .NET 6.0             |  0.4287 ns |         - |
| TypeMapperVersion   | .NET 6.0             |  5.6296 ns |         - |
| DirectAccessVersion | .NET 6.0             |  0.8082 ns |         - |
| TypeMapperString    | .NET 8.0             |  2.2082 ns |         - |
| DirectAccessString  | .NET 8.0             |  2.4111 ns |         - |
| TypeMapperInt       | .NET 8.0             |  3.2728 ns |         - |
| DirectAccessInt     | .NET 8.0             |  0.3883 ns |         - |
| TypeMapperLong      | .NET 8.0             |  3.8684 ns |         - |
| DirectAccessLong    | .NET 8.0             |  0.4725 ns |         - |
| TypeMapperBoolean   | .NET 8.0             |  3.3435 ns |         - |
| DirectAccessBoolean | .NET 8.0             |  0.5594 ns |         - |
| TypeMapperWrapper   | .NET 8.0             |  7.3790 ns |         - |
| DirectAccessWrapper | .NET 8.0             |  0.3211 ns |         - |
| TypeMapperEnum      | .NET 8.0             | 13.0475 ns |         - |
| DirectAccessEnum    | .NET 8.0             |  0.4538 ns |         - |
| TypeMapperVersion   | .NET 8.0             |  2.7849 ns |         - |
| DirectAccessVersion | .NET 8.0             |  0.5274 ns |         - |
| TypeMapperString    | .NET 9.0             |  2.3792 ns |         - |
| DirectAccessString  | .NET 9.0             |  0.4885 ns |         - |
| TypeMapperInt       | .NET 9.0             |  3.0917 ns |         - |
| DirectAccessInt     | .NET 9.0             |  0.4502 ns |         - |
| TypeMapperLong      | .NET 9.0             |  3.0897 ns |         - |
| DirectAccessLong    | .NET 9.0             |  0.4649 ns |         - |
| TypeMapperBoolean   | .NET 9.0             |  3.1282 ns |         - |
| DirectAccessBoolean | .NET 9.0             |  0.4624 ns |         - |
| TypeMapperWrapper   | .NET 9.0             |  7.0187 ns |         - |
| DirectAccessWrapper | .NET 9.0             |  0.5014 ns |         - |
| TypeMapperEnum      | .NET 9.0             | 12.2726 ns |         - |
| DirectAccessEnum    | .NET 9.0             |  0.4705 ns |         - |
| TypeMapperVersion   | .NET 9.0             |  2.2808 ns |         - |
| DirectAccessVersion | .NET 9.0             |  0.4589 ns |         - |
| TypeMapperString    | .NET Framework 4.6.2 | 25.0861 ns |         - |
| DirectAccessString  | .NET Framework 4.6.2 |  0.8296 ns |         - |
| TypeMapperInt       | .NET Framework 4.6.2 | 20.6274 ns |         - |
| DirectAccessInt     | .NET Framework 4.6.2 |  1.2047 ns |         - |
| TypeMapperLong      | .NET Framework 4.6.2 | 23.9126 ns |         - |
| DirectAccessLong    | .NET Framework 4.6.2 |  1.3398 ns |         - |
| TypeMapperBoolean   | .NET Framework 4.6.2 | 23.6110 ns |         - |
| DirectAccessBoolean | .NET Framework 4.6.2 |  1.2686 ns |         - |
| TypeMapperWrapper   | .NET Framework 4.6.2 | 38.5998 ns |         - |
| DirectAccessWrapper | .NET Framework 4.6.2 |  0.8852 ns |         - |
| TypeMapperEnum      | .NET Framework 4.6.2 | 65.2767 ns |      24 B |
| DirectAccessEnum    | .NET Framework 4.6.2 |  0.9250 ns |         - |
| TypeMapperVersion   | .NET Framework 4.6.2 | 20.6124 ns |         - |
| DirectAccessVersion | .NET Framework 4.6.2 |  0.8317 ns |         - |
