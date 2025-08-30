```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.17763.7553/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X 3.39GHz, 2 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FTOCRB : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-DHTNJT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-QIENBV : .NET Framework 4.8 (4.8.4795.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method    | Runtime              | Mean      | Allocated |
|---------- |--------------------- |----------:|----------:|
| Linq      | .NET 8.0             | 16.193 ms |   7.95 MB |
| Compiled  | .NET 8.0             | 16.509 ms |   7.94 MB |
| RawAdoNet | .NET 8.0             | 10.222 ms |   7.94 MB |
| Linq      | .NET 9.0             | 12.143 ms |   7.95 MB |
| Compiled  | .NET 9.0             | 13.246 ms |   7.94 MB |
| RawAdoNet | .NET 9.0             |  9.229 ms |   7.94 MB |
| Linq      | .NET Framework 4.6.2 | 31.532 ms |   7.97 MB |
| Compiled  | .NET Framework 4.6.2 | 32.719 ms |   7.97 MB |
| RawAdoNet | .NET Framework 4.6.2 | 17.571 ms |   7.96 MB |
