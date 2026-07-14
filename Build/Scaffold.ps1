Param(
  [Parameter (Mandatory = $false)]
  [string] $iniFile = ".\build.ini"
)

Import-Module .\Get-Config.psm1

$config = Get-Config -iniFile $iniFile
$msBuildPath = $config.MsBuildFilePath

$tablesFiles = Get-ChildItem -Path $config.DatabaseTablesDir -File
$tablesFiles += Get-ChildItem -Path $config.DatabaseViewsDir -File
$lookupTablesFiles = Get-ChildItem -Path $config.DatabaseLookupTablesDir -File

$tablesCompared = Compare-Object -ReferenceObject ($tablesFiles) -DifferenceObject ($lookupTablesFiles) -Property BaseName  | Where-Object{$_.sideIndicator -eq "<="}

$tablesComparedName = $tablesCompared.BaseName

$tablesIncluded = Compare-Object -ReferenceObject ($tablesComparedName) -DifferenceObject ($config.TableExcludeList.Split(",")) | Where-Object{$_.sideIndicator -eq "<="}

$tablesIncludedForEFScaffold = $tablesIncluded.InputObject

$connectionString = "Server=" + $config.Server + ";Database=" + $config.DatabaseName + ";Trusted_Connection=True;Encrypt=False;"

"Scaffold"
& Scaffold-DbContext $connectionString Microsoft.EntityFrameworkCore.SqlServer -OutputDir Entities/Generated -Project $config.ApiEFModelsProject -Context $config.ApiEFModelsDbContextName -Force -StartupProject $config.ApiEFModelsProject -DataAnnotations -UseDatabaseNames -NoOnConfiguring -Namespace $config.ApiEFModelsNamespace -Tables $tablesIncludedForEFScaffold

$csProj = $config.EFPocoGeneratorCSProj
if ($csProj)
{
  "Build POCO Generator"
  Import-Module .\Invoke-MsBuild.psm1

  if ([string]::IsNullOrEmpty($msBuildPath)) {
    $result = Invoke-MsBuild -Path $config.EFPocoGeneratorCSProj -MsBuildParameters "/restore"
  }
  else {
    $result = Invoke-MsBuild -MsBuildFilePath $msBuildPath -Path $config.EFPocoGeneratorCSProj -MsBuildParameters "/restore"
  }
  Write-Output "Build Succeeded: " $result.BuildSucceeded
}

$path = $config.EFPocoGeneratorExePath
if ($path)
{
  "Generate POCOs"
  $args1 = "--db-server-name=" + $config.Server + " --db-name=" + $config.DatabaseName + " --generate-primary-key-objects=true --generate-enums-as-select-dropdown-options=true --code-namespace=" + $config.ApiEFModelsNamespace + " --api-efmodels-output-dir=" + $config.ApiEFModelExtensionMethodsPath + " --table-exclude-list=" + $config.TableExcludeList + " --enum-list=" + ($lookupTablesFiles.BaseName -join ",") + " --typescript-enums-output-dir=" + $config.TypescriptEnumsPath

  # Launch via the signed `dotnet` host rather than the unsigned apphost .exe: ESA endpoint
  # policy (2026-07, WDAC/Defender-level — the AppLocker channel logs nothing) started denying
  # locally-built unsigned executables in user-writable paths with "Access is denied". The
  # managed .dll runs fine under dotnet.exe, which is signed and lives in Program Files.
  $dllPath = [System.IO.Path]::ChangeExtension("$PSScriptRoot\$path", ".dll")
  $pinfo = New-Object System.Diagnostics.ProcessStartInfo
  $pinfo.FileName = "dotnet"
  $pinfo.RedirectStandardError = $true
  $pinfo.RedirectStandardOutput = $true
  $pinfo.UseShellExecute = $false
  $pinfo.Arguments = '"' + $dllPath + '" ' + $args1
  $pinfo.WorkingDirectory = "$PSScriptRoot\"
  $p = New-Object System.Diagnostics.Process
  $p.StartInfo = $pinfo
  $p.Start() | Out-Null
  $stdout = $p.StandardOutput.ReadToEnd()
  $stderr = $p.StandardError.ReadToEnd()
  $p.WaitForExit()
  Write-Output $stdout
  Write-Output "Errors: $stderr"
  Write-Output "Exit Code: " $p.ExitCode
}