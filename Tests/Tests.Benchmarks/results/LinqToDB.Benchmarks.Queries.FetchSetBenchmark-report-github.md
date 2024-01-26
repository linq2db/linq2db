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
| Method    | Runtime              | Mean     | Allocated |
|---------- |--------------------- |---------:|----------:|
| Linq      | .NET 6.0             | 15.36 ms |   7.95 MB |
| Compiled  | .NET 6.0             | 15.54 ms |   7.94 MB |
| RawAdoNet | .NET 6.0             | 14.68 ms |   7.94 MB |
| Linq      | .NET 7.0             | 14.85 ms |   7.95 MB |
| Compiled  | .NET 7.0             | 15.35 ms |   7.94 MB |
| RawAdoNet | .NET 7.0             | 15.45 ms |   7.94 MB |
| Linq      | .NET Core 3.1        | 16.73 ms |   7.95 MB |
| Compiled  | .NET Core 3.1        | 16.38 ms |   7.94 MB |
| RawAdoNet | .NET Core 3.1        | 15.51 ms |   7.94 MB |
| Linq      | .NET Framework 4.7.2 | 27.28 ms |   7.97 MB |
| Compiled  | .NET Framework 4.7.2 | 30.93 ms |   7.97 MB |
| RawAdoNet | .NET Framework 4.7.2 | 19.20 ms |   7.96 MB |
