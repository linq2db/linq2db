```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5458/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-GXDOCB : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2
  Job-YDFVLV : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-SBTNYY : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method    | Runtime              | Mean      | Allocated |
|---------- |--------------------- |----------:|----------:|
| Linq      | .NET 6.0             | 14.941 ms |   7.95 MB |
| Compiled  | .NET 6.0             | 13.475 ms |   7.94 MB |
| RawAdoNet | .NET 6.0             | 14.688 ms |   7.94 MB |
| Linq      | .NET 8.0             | 17.375 ms |   7.94 MB |
| Compiled  | .NET 8.0             | 13.888 ms |   7.94 MB |
| RawAdoNet | .NET 8.0             |  9.677 ms |   7.94 MB |
| Linq      | .NET Framework 4.6.2 | 25.872 ms |   7.97 MB |
| Compiled  | .NET Framework 4.6.2 | 33.245 ms |   7.97 MB |
| RawAdoNet | .NET Framework 4.6.2 | 18.120 ms |   7.96 MB |
