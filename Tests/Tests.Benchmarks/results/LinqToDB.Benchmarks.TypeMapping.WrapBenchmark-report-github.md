```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                      | Runtime              | Mean        | Allocated |
|---------------------------- |--------------------- |------------:|----------:|
| TypeMapperString            | .NET 6.0             |   5.0992 ns |         - |
| DirectAccessString          | .NET 6.0             |   0.8358 ns |         - |
| TypeMapperWrappedInstance   | .NET 6.0             |  48.7130 ns |      32 B |
| DirectAccessWrappedInstance | .NET 6.0             |   1.0681 ns |         - |
| TypeMapperGetEnumerator     | .NET 6.0             |  54.6890 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 6.0             |  55.8806 ns |      32 B |
| TypeMapperString            | .NET 8.0             |   3.2807 ns |         - |
| DirectAccessString          | .NET 8.0             |   1.4255 ns |         - |
| TypeMapperWrappedInstance   | .NET 8.0             |  32.2343 ns |      32 B |
| DirectAccessWrappedInstance | .NET 8.0             |   1.0774 ns |         - |
| TypeMapperGetEnumerator     | .NET 8.0             |  51.8030 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 8.0             |  48.8543 ns |      32 B |
| TypeMapperString            | .NET 9.0             |   3.1919 ns |         - |
| DirectAccessString          | .NET 9.0             |   1.3585 ns |         - |
| TypeMapperWrappedInstance   | .NET 9.0             |  35.6400 ns |      32 B |
| DirectAccessWrappedInstance | .NET 9.0             |   1.8479 ns |         - |
| TypeMapperGetEnumerator     | .NET 9.0             |  48.2487 ns |      32 B |
| DirectAccessGetEnumerator   | .NET 9.0             |  43.1230 ns |      32 B |
| TypeMapperString            | .NET Framework 4.6.2 |  23.0606 ns |         - |
| DirectAccessString          | .NET Framework 4.6.2 |   0.9562 ns |         - |
| TypeMapperWrappedInstance   | .NET Framework 4.6.2 |  90.0045 ns |      32 B |
| DirectAccessWrappedInstance | .NET Framework 4.6.2 |   0.8538 ns |         - |
| TypeMapperGetEnumerator     | .NET Framework 4.6.2 | 180.5234 ns |      56 B |
| DirectAccessGetEnumerator   | .NET Framework 4.6.2 | 152.4001 ns |      56 B |
