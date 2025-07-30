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
| Method    | Runtime              | Mean        | Allocated |
|---------- |--------------------- |------------:|----------:|
| Linq      | .NET 8.0             | 17,658.3 ns |    6.4 KB |
| Compiled  | .NET 8.0             |  4,239.1 ns |   3.05 KB |
| RawAdoNet | .NET 8.0             |    409.0 ns |   1.48 KB |
| Linq      | .NET 9.0             | 17,196.9 ns |   6.41 KB |
| Compiled  | .NET 9.0             |  3,662.2 ns |   3.05 KB |
| RawAdoNet | .NET 9.0             |    178.1 ns |   1.48 KB |
| Linq      | .NET Framework 4.6.2 | 73,851.4 ns |  11.69 KB |
| Compiled  | .NET Framework 4.6.2 |  9,561.5 ns |   4.42 KB |
| RawAdoNet | .NET Framework 4.6.2 |    763.9 ns |   1.54 KB |
