import sys
from run import execute

phases = [
    ('format-final', ['.dotnet/dotnet.exe', 'format', 'whitespace', '--folder', '.', '--include', 'src/Compilers/CSharp/Portable/Lowering/LocalRewriter/LocalRewriter_CollectionExpression.cs', 'src/Compilers/CSharp/Test/Emit3/Semantics/CollectionExpressionTests.cs']),
    ('build-tests-clean', ['.dotnet/dotnet.exe', 'build', 'src/Compilers/CSharp/Test/Emit3/Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests.csproj', '-c', 'Release', '-f', 'net10.0', '-m:1', '-nr:false', '-p:BuildInParallel=false', '-p:UseSharedCompilation=false', '-p:RunAnalyzersDuringBuild=true', '--no-restore']),
    ('build-compiler-final', ['.dotnet/dotnet.exe', 'build', 'src/Compilers/CSharp/csc/AnyCpu/csc.csproj', '-c', 'Release', '-f', 'net10.0', '-m:1', '-nr:false', '-p:BuildInParallel=false', '-p:UseSharedCompilation=false', '-p:RunAnalyzersDuringBuild=true', '--no-restore']),
    ('tests-clean', ['.dotnet/dotnet.exe', 'test', 'src/Compilers/CSharp/Test/Emit3/Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests.csproj', '-c', 'Release', '-f', 'net10.0', '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~CollectionExpressionTests', '--logger', 'trx;LogFileName=centralized-clean.trx']),
]
for name, args in phases:
    code = execute(name, args)
    if code: sys.exit(code)
