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
| Method    | Runtime              | Mean         | Allocated |
|---------- |--------------------- |-------------:|----------:|
| Linq      | .NET 8.0             |  15,312.6 ns |  13.05 KB |
| Compiled  | .NET 8.0             |  19,594.6 ns |  10.53 KB |
| RawAdoNet | .NET 8.0             |     340.2 ns |   1.48 KB |
| Linq      | .NET 9.0             |  29,557.1 ns |  13.09 KB |
| Compiled  | .NET 9.0             |  16,753.2 ns |  10.56 KB |
| RawAdoNet | .NET 9.0             |     269.7 ns |   1.48 KB |
| Linq      | .NET Framework 4.6.2 | 100,930.9 ns |  16.54 KB |
| Compiled  | .NET Framework 4.6.2 |  49,880.7 ns |   10.7 KB |
| RawAdoNet | .NET Framework 4.6.2 |     708.2 ns |   1.54 KB |
