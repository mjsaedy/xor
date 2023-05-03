@echo off

del *.exe

:: .NET 2
::"C:\Windows\Microsoft.NET\Framework64\v2.0.50727\csc.exe" %*


:: .NET 3.5
:: "C:\Windows\Microsoft.NET\Framework64\v3.5\csc.exe" /nologo /t:library /out:LockedBitmap.dll /unsafe xxxxx.cs
:: "C:\Windows\Microsoft.NET\Framework64\v3.5\csc.exe" /nologo /r:lockedbitmap.dll speed-test.cs
:: "C:\Windows\Microsoft.NET\Framework64\v3.5\csc.exe" /nologo /out:speed35.exe _speed-test-Clamp.cs


:: Visual C# 2013
::"C:\Program Files (x86)\MSBuild\12.0\Bin\csc.exe" %*
::"C:\Program Files (x86)\MSBuild\12.0\Bin\amd64\csc.exe" %*

:: .NET 4 (4.6.1)
:: "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" /out:speed4.exe _speed-test-Clamp.cs
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" "xor.cs" "CmdLineParser (FIXED+ENHANCED).cs" "Console Color.cs" "TaskbarProgress.cs"

copy xor.exe 1k.exe

::pause
