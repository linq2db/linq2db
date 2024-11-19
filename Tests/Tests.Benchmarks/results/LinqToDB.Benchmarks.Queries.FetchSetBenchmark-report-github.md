```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5696/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-VZLGGZ : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2
  Job-AZKKUX : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-TQCFWV : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method    | Runtime              | Mean      | Allocated |
|---------- |--------------------- |----------:|----------:|
| Linq      | .NET 6.0             | 15.512 ms |   7.95 MB |
| Compiled  | .NET 6.0             | 13.890 ms |   7.94 MB |
| RawAdoNet | .NET 6.0             | 15.592 ms |   7.94 MB |
| Linq      | .NET 8.0             | 16.779 ms |   7.94 MB |
| Compiled  | .NET 8.0             | 17.054 ms |   7.94 MB |
| RawAdoNet | .NET 8.0             |  9.903 ms |   7.94 MB |
| Linq      | .NET Framework 4.6.2 | 30.204 ms |   7.97 MB |
| Compiled  | .NET Framework 4.6.2 | 32.940 ms |   7.97 MB |
| RawAdoNet | .NET Framework 4.6.2 | 18.921 ms |   7.97 MB |
