for %%X in (Debug Release) do (
    pushd %~dp0
    MSBuild.exe PKHeX.WinForms/PKHeX.WinForms.csproj /p:Configuration=%%X

    cd ../PKHeX-Plugins
    MSBuild.exe AutoLegalityMod/AutoModPlugins.csproj /p:Configuration=%%X
    robocopy AutoLegalityMod\bin\%%X\net46  ..\PKHeX\PKHeX.WinForms\bin\%%X\net46\plugins\   AutoModPlugins.dll LibUsbDotNet.LibUsbDotNet.dll NtrSharp.dll PKHeX.Core.AutoMod.dll PKHeX.Core.Enhancements.dll PKHeX.Core.Injection.dll


    popd
)
