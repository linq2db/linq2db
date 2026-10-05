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
| Method                                | Runtime              | Mean        | Allocated |
|-------------------------------------- |--------------------- |------------:|----------:|
| TestCastConvertTypeMapper             | .NET 8.0             |   2.8444 ns |         - |
| TestCastConvertDirectAccess           | .NET 8.0             |   0.4601 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 8.0             |  22.1769 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 8.0             |   0.0000 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 8.0             |  21.6984 ns |         - |
| TestDictionaryCastConvertDirectAccess | .NET 8.0             |   0.4379 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 8.0             |   3.2079 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 8.0             |   0.3910 ns |         - |
| TestCastConvertTypeMapper             | .NET 9.0             |   4.0514 ns |         - |
| TestCastConvertDirectAccess           | .NET 9.0             |   0.4650 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 9.0             |  20.8388 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 9.0             |   1.8009 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 9.0             |  14.0538 ns |         - |
| TestDictionaryCastConvertDirectAccess | .NET 9.0             |   0.4393 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 9.0             |   2.5972 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 9.0             |   0.3757 ns |         - |
| TestCastConvertTypeMapper             | .NET Framework 4.6.2 |  23.8261 ns |         - |
| TestCastConvertDirectAccess           | .NET Framework 4.6.2 |   1.3621 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET Framework 4.6.2 |  59.6044 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET Framework 4.6.2 |   1.4441 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET Framework 4.6.2 | 108.0474 ns |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.6.2 |   1.3142 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET Framework 4.6.2 |  21.3492 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET Framework 4.6.2 |   1.4333 ns |         - |
