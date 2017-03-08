@echo off

cd tests\CodeCoverage

nuget restore packages.config -PackagesDirectory .

cd ..
cd ..

dotnet restore  SixLabors.Fonts.sln
dotnet build  SixLabors.Fonts.sln --no-incremental -c debug /p:codecov=true
 
rem The -threshold options prevents this taking ages...
tests\CodeCoverage\OpenCover.4.6.519\tools\OpenCover.Console.exe -target:"dotnet.exe" -targetargs:"test tests\SixLabors.Fonts.Tests\SixLabors.Fonts.Tests.csproj --no-build -c debug" -searchdirs:"tests\SixLabors.Fonts.Tests\bin\Release\netcoreapp1.1" -register:user -output:.\SixLabors.Fonts.Coverage.xml -hideskipped:All -returntargetcode -oldStyle -filter:"+[SixLabors.Fonts*]*" 

if %errorlevel% neq 0 exit /b %errorlevel%

SET PATH=C:\\Python34;C:\\Python34\\Scripts;%PATH%
pip install codecov
codecov -f "SixLabors.Fonts.Coverage.xml"