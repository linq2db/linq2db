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
| TypeMapperString            | .NET 8.0             |   3.4346 ns |         - |
| DirectAccessString          | .NET 8.0             |   1.8940 ns |         - |
| TypeMapperWrappedInstance   | .NET 8.0             |  35.7311 ns |      32 B |
| DirectAccessWrappedInstance | .NET 8.0             |   1.5332 ns |         - |
| TypeMapperGetEnumerator     | .NET 8.0             |  53.3593 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 8.0             |  19.8246 ns |      32 B |
| TypeMapperString            | .NET 9.0             |   1.7530 ns |         - |
| DirectAccessString          | .NET 9.0             |   1.4952 ns |         - |
| TypeMapperWrappedInstance   | .NET 9.0             |  35.5558 ns |      32 B |
| DirectAccessWrappedInstance | .NET 9.0             |   0.8341 ns |         - |
| TypeMapperGetEnumerator     | .NET 9.0             |  40.5225 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 9.0             |  37.1403 ns |      32 B |
| TypeMapperString            | .NET Framework 4.6.2 |  23.2695 ns |         - |
| DirectAccessString          | .NET Framework 4.6.2 |   3.3386 ns |         - |
| TypeMapperWrappedInstance   | .NET Framework 4.6.2 |  88.1164 ns |      32 B |
| DirectAccessWrappedInstance | .NET Framework 4.6.2 |   1.1836 ns |         - |
| TypeMapperGetEnumerator     | .NET Framework 4.6.2 | 130.7879 ns |      56 B |
| DirectAccessGetEnumerator   | .NET Framework 4.6.2 | 127.2017 ns |      56 B |
