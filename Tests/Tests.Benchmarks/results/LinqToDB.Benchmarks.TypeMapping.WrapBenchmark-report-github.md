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
| Method                      | Runtime              | Mean        | Allocated |
|---------------------------- |--------------------- |------------:|----------:|
| TypeMapperString            | .NET 6.0             |   2.2438 ns |         - |
| DirectAccessString          | .NET 6.0             |   1.3245 ns |         - |
| TypeMapperWrappedInstance   | .NET 6.0             |  27.1528 ns |      32 B |
| DirectAccessWrappedInstance | .NET 6.0             |   0.8133 ns |         - |
| TypeMapperGetEnumerator     | .NET 6.0             |  63.4075 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 6.0             |  34.5480 ns |      32 B |
| TypeMapperString            | .NET 7.0             |   6.0528 ns |         - |
| DirectAccessString          | .NET 7.0             |   2.4339 ns |         - |
| TypeMapperWrappedInstance   | .NET 7.0             |  45.8062 ns |      32 B |
| DirectAccessWrappedInstance | .NET 7.0             |   1.6763 ns |         - |
| TypeMapperGetEnumerator     | .NET 7.0             |  57.8984 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 7.0             |  52.4231 ns |      32 B |
| TypeMapperString            | .NET Core 3.1        |   5.7207 ns |         - |
| DirectAccessString          | .NET Core 3.1        |   1.9452 ns |         - |
| TypeMapperWrappedInstance   | .NET Core 3.1        |  53.8521 ns |      32 B |
| DirectAccessWrappedInstance | .NET Core 3.1        |   3.5635 ns |         - |
| TypeMapperGetEnumerator     | .NET Core 3.1        | 132.0469 ns |      32 B |
| DirectAccessGetEnumerator   | .NET Core 3.1        | 122.9845 ns |      32 B |
| TypeMapperString            | .NET Framework 4.7.2 |  23.7130 ns |         - |
| DirectAccessString          | .NET Framework 4.7.2 |   1.3215 ns |         - |
| TypeMapperWrappedInstance   | .NET Framework 4.7.2 |  89.4689 ns |      32 B |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   0.6974 ns |         - |
| TypeMapperGetEnumerator     | .NET Framework 4.7.2 | 185.9241 ns |      56 B |
| DirectAccessGetEnumerator   | .NET Framework 4.7.2 | 148.6151 ns |      56 B |
