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
| Method                  | Runtime              | Mean       | Allocated |
|------------------------ |--------------------- |-----------:|----------:|
| TypeMapperAsEnum        | .NET 6.0             |  9.5528 ns |         - |
| DirectAccessAsEnum      | .NET 6.0             |  0.9188 ns |         - |
| TypeMapperAsKnownEnum   | .NET 6.0             |  1.9616 ns |         - |
| DirectAccessAsKnownEnum | .NET 6.0             |  0.9644 ns |         - |
| TypeMapperAsString      | .NET 6.0             |  5.5446 ns |         - |
| DirectAccessAsString    | .NET 6.0             |  3.7148 ns |         - |
| TypeMapperAsEnum        | .NET 8.0             | 10.8193 ns |         - |
| DirectAccessAsEnum      | .NET 8.0             |  0.4799 ns |         - |
| TypeMapperAsKnownEnum   | .NET 8.0             |  1.8920 ns |         - |
| DirectAccessAsKnownEnum | .NET 8.0             |  0.4706 ns |         - |
| TypeMapperAsString      | .NET 8.0             |  5.1169 ns |         - |
| DirectAccessAsString    | .NET 8.0             |  3.7451 ns |         - |
| TypeMapperAsEnum        | .NET 9.0             | 10.3600 ns |         - |
| DirectAccessAsEnum      | .NET 9.0             |  0.9626 ns |         - |
| TypeMapperAsKnownEnum   | .NET 9.0             |  2.3637 ns |         - |
| DirectAccessAsKnownEnum | .NET 9.0             |  0.4952 ns |         - |
| TypeMapperAsString      | .NET 9.0             |  5.1638 ns |         - |
| DirectAccessAsString    | .NET 9.0             |  4.2105 ns |         - |
| TypeMapperAsEnum        | .NET Framework 4.6.2 | 29.9702 ns |         - |
| DirectAccessAsEnum      | .NET Framework 4.6.2 |  0.8815 ns |         - |
| TypeMapperAsKnownEnum   | .NET Framework 4.6.2 | 10.1684 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.6.2 |  0.8788 ns |         - |
| TypeMapperAsString      | .NET Framework 4.6.2 | 11.6220 ns |         - |
| DirectAccessAsString    | .NET Framework 4.6.2 |  3.5221 ns |         - |
