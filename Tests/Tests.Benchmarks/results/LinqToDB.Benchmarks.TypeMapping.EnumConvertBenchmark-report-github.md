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
| TestCastConvertTypeMapper             | .NET 8.0             |   1.5524 ns |         - |
| TestCastConvertDirectAccess           | .NET 8.0             |   1.7654 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 8.0             |  20.2274 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 8.0             |   1.6775 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 8.0             |  21.3362 ns |         - |
| TestDictionaryCastConvertDirectAccess | .NET 8.0             |   0.4489 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 8.0             |   2.7060 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 8.0             |   0.4190 ns |         - |
| TestCastConvertTypeMapper             | .NET 9.0             |   2.4274 ns |         - |
| TestCastConvertDirectAccess           | .NET 9.0             |   0.9388 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 9.0             |  18.8217 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 9.0             |   0.3917 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 9.0             |  15.7817 ns |         - |
| TestDictionaryCastConvertDirectAccess | .NET 9.0             |   0.4565 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 9.0             |   1.2689 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 9.0             |   0.4406 ns |         - |
| TestCastConvertTypeMapper             | .NET Framework 4.6.2 |  15.1217 ns |         - |
| TestCastConvertDirectAccess           | .NET Framework 4.6.2 |   0.8872 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET Framework 4.6.2 |  65.1004 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET Framework 4.6.2 |   0.9127 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET Framework 4.6.2 | 109.4963 ns |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.6.2 |   0.8971 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET Framework 4.6.2 |  21.1430 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET Framework 4.6.2 |   0.8962 ns |         - |
