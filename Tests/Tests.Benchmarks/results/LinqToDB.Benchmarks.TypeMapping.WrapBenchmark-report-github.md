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
| Method                      | Runtime              | Mean        | Allocated |
|---------------------------- |--------------------- |------------:|----------:|
| TypeMapperString            | .NET 8.0             |   2.8599 ns |         - |
| DirectAccessString          | .NET 8.0             |   2.8802 ns |         - |
| TypeMapperWrappedInstance   | .NET 8.0             |  35.2638 ns |      32 B |
| DirectAccessWrappedInstance | .NET 8.0             |   1.7114 ns |         - |
| TypeMapperGetEnumerator     | .NET 8.0             |  51.9164 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 8.0             |  49.9917 ns |      32 B |
| TypeMapperString            | .NET 9.0             |   3.2645 ns |         - |
| DirectAccessString          | .NET 9.0             |   1.3496 ns |         - |
| TypeMapperWrappedInstance   | .NET 9.0             |  34.3376 ns |      32 B |
| DirectAccessWrappedInstance | .NET 9.0             |   1.7621 ns |         - |
| TypeMapperGetEnumerator     | .NET 9.0             |  47.2077 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 9.0             |  43.7619 ns |      32 B |
| TypeMapperString            | .NET Framework 4.6.2 |  22.3599 ns |         - |
| DirectAccessString          | .NET Framework 4.6.2 |   0.8622 ns |         - |
| TypeMapperWrappedInstance   | .NET Framework 4.6.2 |  88.8039 ns |      32 B |
| DirectAccessWrappedInstance | .NET Framework 4.6.2 |   3.2147 ns |         - |
| TypeMapperGetEnumerator     | .NET Framework 4.6.2 | 190.1164 ns |      56 B |
| DirectAccessGetEnumerator   | .NET Framework 4.6.2 | 159.5740 ns |      56 B |
