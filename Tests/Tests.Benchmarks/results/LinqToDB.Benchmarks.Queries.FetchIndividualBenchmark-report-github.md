```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.5328/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-KJWIMT : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GULBRG : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-LRGNRQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-SJROSW : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method    | Runtime              | Mean        | Allocated |
|---------- |--------------------- |------------:|----------:|
| Linq      | .NET 6.0             | 39,290.1 ns |   9.36 KB |
| Compiled  | .NET 6.0             |  5,963.5 ns |   2.96 KB |
| RawAdoNet | .NET 6.0             |    617.0 ns |   1.48 KB |
| Linq      | .NET 7.0             | 24,062.7 ns |   6.17 KB |
| Compiled  | .NET 7.0             |  4,861.1 ns |   2.95 KB |
| RawAdoNet | .NET 7.0             |    594.6 ns |   1.48 KB |
| Linq      | .NET Core 3.1        | 50,330.1 ns |   10.5 KB |
| Compiled  | .NET Core 3.1        |  8,775.7 ns |   3.95 KB |
| RawAdoNet | .NET Core 3.1        |    330.9 ns |   1.48 KB |
| Linq      | .NET Framework 4.7.2 | 73,258.7 ns |   11.4 KB |
| Compiled  | .NET Framework 4.7.2 |  4,920.5 ns |   4.29 KB |
| RawAdoNet | .NET Framework 4.7.2 |    381.7 ns |   1.54 KB |
