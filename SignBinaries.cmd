@ECHO OFF
START /wait cmd /c %~dp0SignCode.cmd %~dp0bin\Handlers.dll
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\Modules.dll
START /wait cmd /c %~dp0SignCode.cmd %~dp0bin\PipeClient.dll
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\PipeServer.dll
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\FOGService.exe
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\FOGUpdateHelper.exe
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\FOGUpdateWaiter.exe
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\FOGUserService.exe
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\FOGTray.exe
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\Debugger.exe
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\libeay32.dll
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\ManagedOpenSsl.dll
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\Microsoft.Win32.TaskScheduler.dll
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\ssleay32.dll
START /wait  cmd /c %~dp0SignCode.cmd %~dp0bin\System.Threading.Tasks.NET35.dll
