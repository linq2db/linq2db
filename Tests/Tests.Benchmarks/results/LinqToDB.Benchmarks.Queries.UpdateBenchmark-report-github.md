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
| Method             | Runtime              | Mean         | Allocated |
|------------------- |--------------------- |-------------:|----------:|
| LinqSet            | .NET 6.0             | 284,456.8 ns |   73392 B |
| LinqObject         | .NET 6.0             | 197,194.8 ns |   47776 B |
| Object             | .NET 6.0             |  25,446.4 ns |   22672 B |
| CompiledLinqSet    | .NET 6.0             |  73,981.2 ns |   25504 B |
| CompiledLinqObject | .NET 6.0             |  75,100.1 ns |   25504 B |
| RawAdoNet          | .NET 6.0             |     244.1 ns |     712 B |
| LinqSet            | .NET 8.0             | 156,078.4 ns |   54272 B |
| LinqObject         | .NET 8.0             | 115,873.8 ns |   44368 B |
| Object             | .NET 8.0             |  35,594.2 ns |   22672 B |
| CompiledLinqSet    | .NET 8.0             |  46,778.4 ns |   25504 B |
| CompiledLinqObject | .NET 8.0             |  45,759.3 ns |   25504 B |
| RawAdoNet          | .NET 8.0             |     225.1 ns |     712 B |
| LinqSet            | .NET 9.0             |  62,188.6 ns |   54257 B |
| LinqObject         | .NET 9.0             | 101,757.7 ns |   44257 B |
| Object             | .NET 9.0             |  14,179.4 ns |   22624 B |
| CompiledLinqSet    | .NET 9.0             |  33,921.3 ns |   25456 B |
| CompiledLinqObject | .NET 9.0             |  40,308.0 ns |   25456 B |
| RawAdoNet          | .NET 9.0             |     193.0 ns |     712 B |
| LinqSet            | .NET Framework 4.6.2 | 450,290.8 ns |   81889 B |
| LinqObject         | .NET Framework 4.6.2 | 285,827.9 ns |   58653 B |
| Object             | .NET Framework 4.6.2 |  80,014.5 ns |   23398 B |
| CompiledLinqSet    | .NET Framework 4.6.2 |  98,837.6 ns |   25932 B |
| CompiledLinqObject | .NET Framework 4.6.2 |  99,868.2 ns |   25932 B |
| RawAdoNet          | .NET Framework 4.6.2 |     720.7 ns |     810 B |
