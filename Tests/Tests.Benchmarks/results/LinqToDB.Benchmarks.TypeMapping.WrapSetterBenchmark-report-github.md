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
| Method              | Runtime              | Mean       | Allocated |
|-------------------- |--------------------- |-----------:|----------:|
| TypeMapperString    | .NET 6.0             |  7.7556 ns |         - |
| DirectAccessString  | .NET 6.0             |  4.2323 ns |         - |
| TypeMapperInt       | .NET 6.0             |  5.0174 ns |         - |
| DirectAccessInt     | .NET 6.0             |  0.0000 ns |         - |
| TypeMapperBoolean   | .NET 6.0             |  5.0943 ns |         - |
| DirectAccessBoolean | .NET 6.0             |  0.7785 ns |         - |
| TypeMapperWrapper   | .NET 6.0             |  9.2182 ns |         - |
| DirectAccessWrapper | .NET 6.0             |  3.7090 ns |         - |
| TypeMapperString    | .NET 8.0             |  6.5082 ns |         - |
| DirectAccessString  | .NET 8.0             |  3.7161 ns |         - |
| TypeMapperInt       | .NET 8.0             |  2.9356 ns |         - |
| DirectAccessInt     | .NET 8.0             |  0.4621 ns |         - |
| TypeMapperBoolean   | .NET 8.0             |  2.6607 ns |         - |
| DirectAccessBoolean | .NET 8.0             |  0.0000 ns |         - |
| TypeMapperWrapper   | .NET 8.0             |  7.9956 ns |         - |
| DirectAccessWrapper | .NET 8.0             |  4.2157 ns |         - |
| TypeMapperString    | .NET 9.0             |  5.4897 ns |         - |
| DirectAccessString  | .NET 9.0             |  4.2091 ns |         - |
| TypeMapperInt       | .NET 9.0             |  2.4420 ns |         - |
| DirectAccessInt     | .NET 9.0             |  0.4989 ns |         - |
| TypeMapperBoolean   | .NET 9.0             |  2.8489 ns |         - |
| DirectAccessBoolean | .NET 9.0             |  1.3131 ns |         - |
| TypeMapperWrapper   | .NET 9.0             |  7.3005 ns |         - |
| DirectAccessWrapper | .NET 9.0             |  4.2012 ns |         - |
| TypeMapperString    | .NET Framework 4.6.2 | 25.5255 ns |         - |
| DirectAccessString  | .NET Framework 4.6.2 |  5.0918 ns |         - |
| TypeMapperInt       | .NET Framework 4.6.2 | 20.1585 ns |         - |
| DirectAccessInt     | .NET Framework 4.6.2 |  1.0337 ns |         - |
| TypeMapperBoolean   | .NET Framework 4.6.2 | 17.4435 ns |         - |
| DirectAccessBoolean | .NET Framework 4.6.2 |  0.9459 ns |         - |
| TypeMapperWrapper   | .NET Framework 4.6.2 | 34.1014 ns |         - |
| DirectAccessWrapper | .NET Framework 4.6.2 |  3.6758 ns |         - |
