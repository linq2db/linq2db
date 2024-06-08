```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                                | Runtime              | Mean       | Allocated |
|-------------------------------------- |--------------------- |-----------:|----------:|
| TestCastConvertTypeMapper             | .NET 6.0             |  5.0625 ns |         - |
| TestCastConvertDirectAccess           | .NET 6.0             |  0.9010 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 6.0             | 23.8742 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 6.0             |  0.9159 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 6.0             | 49.0737 ns |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET 6.0             |  0.9461 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 6.0             |  3.5878 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 6.0             |  0.9479 ns |         - |
| TestCastConvertTypeMapper             | .NET 7.0             |  4.1129 ns |         - |
| TestCastConvertDirectAccess           | .NET 7.0             |  0.0000 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET 7.0             | 24.0047 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET 7.0             |  0.4922 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET 7.0             | 18.8953 ns |         - |
| TestDictionaryCastConvertDirectAccess | .NET 7.0             |  0.4718 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET 7.0             |  4.2282 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET 7.0             |  0.4852 ns |         - |
| TestCastConvertTypeMapper             | .NET Core 3.1        |  6.1614 ns |         - |
| TestCastConvertDirectAccess           | .NET Core 3.1        |  1.3931 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET Core 3.1        | 28.9017 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET Core 3.1        |  0.9379 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET Core 3.1        | 63.5673 ns |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Core 3.1        |  0.8548 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET Core 3.1        |  2.2823 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET Core 3.1        |  0.8985 ns |         - |
| TestCastConvertTypeMapper             | .NET Framework 4.7.2 | 25.1164 ns |         - |
| TestCastConvertDirectAccess           | .NET Framework 4.7.2 |  1.4255 ns |         - |
| TestDictionaryConvertTypeMapper       | .NET Framework 4.7.2 | 47.5827 ns |         - |
| TestDictionaryConvertDirectAccess     | .NET Framework 4.7.2 |  1.3713 ns |         - |
| TestDictionaryCastConvertTypeMapper   | .NET Framework 4.7.2 | 94.7129 ns |      48 B |
| TestDictionaryCastConvertDirectAccess | .NET Framework 4.7.2 |  1.3633 ns |         - |
| TestFlagsCastConvertTypeMapper        | .NET Framework 4.7.2 | 24.9646 ns |         - |
| TestFlagsCastConvertDirectAccess      | .NET Framework 4.7.2 |  1.3710 ns |         - |
