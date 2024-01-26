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
| Method    | Runtime              | Mean        | Allocated |
|---------- |--------------------- |------------:|----------:|
| Linq      | .NET 6.0             | 36,154.6 ns |   9.28 KB |
| Compiled  | .NET 6.0             |  5,355.9 ns |   2.88 KB |
| RawAdoNet | .NET 6.0             |    633.0 ns |   1.48 KB |
| Linq      | .NET 7.0             | 19,643.2 ns |   6.09 KB |
| Compiled  | .NET 7.0             |  5,456.0 ns |   2.88 KB |
| RawAdoNet | .NET 7.0             |    552.5 ns |   1.48 KB |
| Linq      | .NET Core 3.1        | 47,348.6 ns |   9.42 KB |
| Compiled  | .NET Core 3.1        |  6,834.9 ns |   2.87 KB |
| RawAdoNet | .NET Core 3.1        |    524.2 ns |   1.48 KB |
| Linq      | .NET Framework 4.7.2 | 70,583.2 ns |  10.63 KB |
| Compiled  | .NET Framework 4.7.2 |  8,907.2 ns |   3.14 KB |
| RawAdoNet | .NET Framework 4.7.2 |    401.7 ns |   1.54 KB |
