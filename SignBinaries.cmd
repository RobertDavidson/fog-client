@ECHO OFF
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\AbstractService.dll
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\Handlers.dll
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\Modules.dll
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\PipeClient.dll
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\PipeServer.dll
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\FOGService.exe
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\FOGUpdateHelper.exe
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\FOGUpdateWaiter.exe
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\FOGUserService.exe
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\FOGTray.exe
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\Debugger.exe
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\Microsoft.Win32.TaskScheduler.dll
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\System.Threading.Tasks.NET35.dll
START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\CertificateManager.exe

START /wait /B cmd /c %~dp0SignCode.cmd %~dp0bin\FOGService.msi