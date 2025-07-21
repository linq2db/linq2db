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
| Method        | Runtime              | Mean       | Allocated |
|-------------- |--------------------- |-----------:|----------:|
| Linq          | .NET 8.0             | 1,113.4 μs |   1.26 MB |
| LinqAsync     | .NET 8.0             | 1,466.2 μs |   1.26 MB |
| Compiled      | .NET 8.0             | 1,012.6 μs |   1.26 MB |
| CompiledAsync | .NET 8.0             | 1,684.5 μs |   1.26 MB |
| Linq          | .NET 9.0             | 1,171.6 μs |   1.26 MB |
| LinqAsync     | .NET 9.0             | 1,450.5 μs |   1.26 MB |
| Compiled      | .NET 9.0             |   935.0 μs |   1.26 MB |
| CompiledAsync | .NET 9.0             | 1,252.9 μs |   1.26 MB |
| Linq          | .NET Framework 4.6.2 | 3,119.5 μs |   1.28 MB |
| LinqAsync     | .NET Framework 4.6.2 | 5,754.2 μs |   1.53 MB |
| Compiled      | .NET Framework 4.6.2 | 2,351.4 μs |   1.27 MB |
| CompiledAsync | .NET Framework 4.6.2 | 4,916.4 μs |   1.52 MB |
