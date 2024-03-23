```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5458/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-GXDOCB : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2
  Job-YDFVLV : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-SBTNYY : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method             | Runtime              | Mean         | Allocated |
|------------------- |--------------------- |-------------:|----------:|
| LinqSet            | .NET 6.0             | 187,554.0 ns |   58928 B |
| LinqObject         | .NET 6.0             | 102,702.9 ns |   31280 B |
| Object             | .NET 6.0             |  13,229.8 ns |    8304 B |
| CompiledLinqSet    | .NET 6.0             |  10,906.7 ns |    8064 B |
| CompiledLinqObject | .NET 6.0             |  12,112.2 ns |    8064 B |
| RawAdoNet          | .NET 6.0             |     262.0 ns |     712 B |
| LinqSet            | .NET 8.0             | 100,158.3 ns |   37392 B |
| LinqObject         | .NET 8.0             |  63,310.3 ns |   27168 B |
| Object             | .NET 8.0             |   8,463.3 ns |    8304 B |
| CompiledLinqSet    | .NET 8.0             |   7,679.9 ns |    8064 B |
| CompiledLinqObject | .NET 8.0             |   7,943.1 ns |    8064 B |
| RawAdoNet          | .NET 8.0             |     221.6 ns |     712 B |
| LinqSet            | .NET Framework 4.6.2 | 325,068.1 ns |   64367 B |
| LinqObject         | .NET Framework 4.6.2 | 173,174.2 ns |   41783 B |
| Object             | .NET Framework 4.6.2 |  22,378.1 ns |    8714 B |
| CompiledLinqSet    | .NET Framework 4.6.2 |   8,435.6 ns |    8248 B |
| CompiledLinqObject | .NET Framework 4.6.2 |  19,407.1 ns |    8248 B |
| RawAdoNet          | .NET Framework 4.6.2 |     799.6 ns |     810 B |
