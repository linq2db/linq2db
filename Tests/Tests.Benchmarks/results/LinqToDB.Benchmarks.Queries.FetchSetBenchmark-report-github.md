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
| Method    | Runtime              | Mean      | Allocated |
|---------- |--------------------- |----------:|----------:|
| Linq      | .NET 6.0             | 15.477 ms |   7.95 MB |
| Compiled  | .NET 6.0             | 15.523 ms |   7.94 MB |
| RawAdoNet | .NET 6.0             | 14.559 ms |   7.94 MB |
| Linq      | .NET 7.0             | 16.478 ms |   7.95 MB |
| Compiled  | .NET 7.0             | 15.324 ms |   7.94 MB |
| RawAdoNet | .NET 7.0             | 15.439 ms |   7.94 MB |
| Linq      | .NET Core 3.1        | 16.952 ms |   7.95 MB |
| Compiled  | .NET Core 3.1        | 16.064 ms |   7.95 MB |
| RawAdoNet | .NET Core 3.1        |  9.452 ms |   7.94 MB |
| Linq      | .NET Framework 4.7.2 | 33.360 ms |   7.97 MB |
| Compiled  | .NET Framework 4.7.2 | 32.833 ms |   7.97 MB |
| RawAdoNet | .NET Framework 4.7.2 | 18.937 ms |   7.97 MB |
