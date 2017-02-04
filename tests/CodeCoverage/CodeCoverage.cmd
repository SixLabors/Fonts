@echo off

cd tests\CodeCoverage

nuget restore packages.config -PackagesDirectory .

cd ..\SixLabors.Shapes.Tests

dotnet restore

cd ..
cd ..

rem The -threshold options prevents this taking ages...
tests\CodeCoverage\OpenCover.4.6.519\tools\OpenCover.Console.exe -target:"C:\Program Files\dotnet\dotnet.exe" -targetargs:"test tests\SixLabors.Fonts.Tests -c Release -f net451" -threshold:10 -register:user -filter:"+[SixLabors.Shapes*]*" -excludebyattribute:*.ExcludeFromCodeCoverage* -hideskipped:All -returntargetcode -output:.\SixLabors.Shapes.Coverage.xml

if %errorlevel% neq 0 exit /b %errorlevel%

SET PATH=C:\\Python34;C:\\Python34\\Scripts;%PATH%
pip install codecov
codecov -f "SixLabors.Shapes.Coverage.xml"