:: 
:: Bootstrap MSBuild support for Visual Studio 2010
:: 

set ANDROID_PLUS_PLUS=%CD%\..\

"%ANDROID_PLUS_PLUS%\msbuild\bin\AndroidPlusPlus.MsBuild.Exporter.exe" --template-dir "%ANDROID_PLUS_PLUS%/msbuild/scripts/" --vs-version 2010