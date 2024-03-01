Param(
  [string]$Branch,
  [string]$BranchIsDefault = '',
  [string]$NugetPreReleaseParameter,
  [int]$CustomRunCount = 0,
  [bool]$AutoUpdateBootstrap = $true,
  [string]$SystemUtilityDERBSVersion = "3.13.214"
)

# To use in TeamCity, add the following parameters to the bootstrap.ps1 build step:
#
# For SVN Builds
# -Branch "%Repo-Path%" -BranchIsDefault "svn" -NugetPreReleaseParameter "%NugetPreReleaseParameter%"
#
# For GIT Builds
# -Branch "%teamcity.build.branch%" -BranchIsDefault "%teamcity.build.branch.is_default%" -NugetPreReleaseParameter "%NugetPreReleaseParameter%"
#
# Add the following parameters to the build parameters list:
# 
# Name: NugetPreReleaseParameter
# Value: <Empty>
#
# Note: If you do not want to use this feature, do not pass the parameters in the build step.

$standBuildProcess = $true;

# PowerShell v2 compatable
$currentDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
If ([String]::IsNullOrEmpty($currentDir)) {
  $currentDir = $pwd.Path;
}

$stopWatch = [system.diagnostics.stopwatch]::startNew();
$customNugetToolsFile = "NugetTools.include";
$nugetConfigFile = Join-Path $env:APPDATA 'NuGet\NuGet.Config'; 
$toolsDir = Join-Path $currentDir "tools";
$nugetDir = Join-Path $toolsDir "Nuget";
$nuget = Join-Path $nugetDir "Nuget.exe";
$configDir = Join-Path $currentDir "config\";
$bootstrapVersionFilename = 'BuildSystem.version';
$nugetLibrarySource = 'http://nugetlib.mgsops.net/';
$nugetCachingServer = 'http://nugetorg.mgsops.net/';
$derbsNugetPackageName = 'System.Utility.DERBS.Bootstrap';
$greatNewLine = "$([Environment]::NewLine)$([Environment]::NewLine)$([Environment]::NewLine)$([Environment]::NewLine)";
$filesRelatedToBuildThatCanBeAutoCommitted = @("bootstrap.ps1", "build.bat", "build.Config.bat", "BuildSystem.version", "SetupBuild.ps1", "SetupBuild.bat");

$Error.Clear();

Function Invoke-DotNetWebRequest ($Uri) {
  $request = [System.Net.WebRequest]::Create($Uri)
  $request.Method="GET"
  $request.ContentType = "application/xml";
  $response = $request.GetResponse();
  $responseStream = $response.GetResponseStream();
  $responseStreamReader = new-object System.IO.StreamReader $responseStream; 
  return $responseStreamReader.ReadToEnd();
}
Function Get-LatestBootstrapVersion {
  try {
    $packageInfo = ([Xml](Invoke-DotNetWebRequest "http://nugetlib.mgsops.net/api/v2/package-versions/$($derbsNugetPackageName)?includePrerelease=false"))
    $versions = $packageInfo.html.body.span;
    return $versions[$versions.length - 1];
  }
  catch {
    Write-Warning ("Unable to get latest $($derbsNugetPackageName) version $($Error[-1].Exception.Message)");
  }
  return '';
}
Function Get-ShouldUpdateBootstrap {
  If ($latestBootstrapVersion -eq '') {
    return $false; # if I can't find the latest, don't go further.
  }

  return ($currentBootstrapVersion -ne $latestBootstrapVersion);
}

Function New-Junction {
  Param (
    [string]$Link, 
    [string]$Target, 
    [bool]$IsDirectory = $true
    )

  $command = "mklink";
  If ($IsDirectory){
    $command = "$($command) /D";
  }
  $command = "$($command) /J $($Link) $($Target)";
  & cmd.exe /c $command;
}

Function Install-Tool {
  Param (
    $ID, 
    $Version, 
    $InstallToFolderName, 
    $InstallDir, 
    $Source, 
    $ExcludeVersion = $false, 
    $ExtraCommandParameters = "", 
    $Comment
  )

  If ([String]::IsNullOrEmpty($ID)) {
    Write-Error "Nuget Tool Id is missing!!";
    Exit 99;
  }

  If ([String]::IsNullOrEmpty($InstallDir)) {
    $InstallDir = ".\"; 
  }
  If ([String]::IsNullOrEmpty($ExcludeVersion)) {
    $ExcludeVersion = $false; 
  }
  If ([String]::IsNullOrEmpty($ExtraCommandParameters)) {
    $ExtraCommandParameters = ""; 
  }
  
  If (-not($InstallDir.StartsWith($currentDir))) {
    $InstallDir = Join-Path $currentDir $InstallDir;
  } 

  If (-not(Test-Path $InstallDir)) {
    Write-Error "Mmmm, Seems like $($InstallDir) does not exist!";
    Exit 88;
  }

  $PathOfInstallation = "$($ID).$($Version)";
  
  PushD $InstallDir;

  $builtCommand = "`"$nuget`" install `"$ID`" ";

  If ($Version -ne $null) {
    $builtCommand += "-Version `"$Version`" ";
  } ElseIf ($InstallToFolderName -ne $null) {
    $ExcludeVersion = $true;
  }

  If ($ExcludeVersion) {
    $builtCommand += "-ExcludeVersion ";
    $PathOfInstallation = "$($ID)";
  }
  If ($Source -ne $null) {
    $builtCommand += "-Source $Source ";
  }
  
  If ((Test-Path $PathOfInstallation) -OR (-not([String]::IsNullOrEmpty($InstallToFolderName)) -AND (Test-Path $InstallToFolderName))) {
    Write-Warning "Tool $($ID) is already installed, skipping";
    PopD;
    Return;
  }

  If ($Comment -ne $null) {
    Write-Warning $Comment; 
  }

  $builtCommand += " -NonInteractive $($ExtraCommandParameters)";
  Write-Output "Executing install CMD: $($builtCommand)";
  Invoke-Expression ". $($builtCommand)";

  If (-not([String]::IsNullOrEmpty($InstallToFolderName))) {
    Write-Output "Creating Junction from $($PathOfInstallation) > $($InstallToFolderName)";
    New-Junction $InstallToFolderName $PathOfInstallation;
  }
  
  Get-ChildItem -Filter "$($ID).*.nupkg" -Path $PathOfInstallation | Remove-Item -Force;

  PopD;
}
Function Tool-Required ($XmlNodesToValidateAgainst, $PropertyName, [String[]]$SearchKeys) {
  If ($XmlNodesToValidateAgainst -eq $null) {
    Write-Warning "No object to compare to so defaulting to install!";
    Return $true; # If there was no targets file to compare to, we need to be safe and just load the tool
  }
  
  ForEach ($import in $XmlNodesToValidateAgainst) {
    ForEach ($searchKey in $SearchKeys) {
      If ($import.$PropertyName -match $searchKey) {
        Write-Warning "Cool, We should install this...";
        Return $true;
      }
    }
  }
  
  Write-Warning "Cool, We are not going install this...";
  Return $false;
}


If (-not(Test-Path $nugetConfigFile)) {
  Write-Warning "You don't have a valid Nuget config in '$nugetConfigFile'.";
}

[xml]$NugetConfig = Get-Content $nugetConfigFile;

If ((($NugetConfig.configuration.packageSources.add) | Where-Object { $_.value -like '*nugetlib*' }).Count -eq 0) {
  Write-Warning "You aren't referencing $nugetLibrarySource in your nuget config, this can cause issues.";
}

If ($BranchIsDefault -ne '') {
  If ($BranchIsDefault -eq 'true' -or $Branch -eq 'trunk') {
    Write-Host "Removing branch version parameter '$NugetPreReleaseParameter' -> ''";
    "##teamcity[setParameter name='NugetPreReleaseParameter' value='']"
  } 
  Else {
    [regex]$removeSymbolsRegex = '[\W_]';
    [regex]$removeSymbolsRegexForStartingDigit = '^\d';
    
    $Branch = $Branch.replace("branches/", "");
    $Branch = $removeSymbolsRegex.Replace($Branch, "");
    
    If ($removeSymbolsRegexForStartingDigit.Match($Branch).Success) {
      $Branch = ('b' + $Branch);
    }
    $BranchNameMaxLength = [System.Math]::Min(20, $Branch.Length);
    $Branch = $Branch.substring(0, $BranchNameMaxLength);
    
    If ($NugetPreReleaseParameter -eq '') {
      Write-Host "Adding branch version parameter '$NugetPreReleaseParameter' -> '-$Branch'";
      "##teamcity[setParameter name='NugetPreReleaseParameter' value='-$Branch']"
    }
    Else {
      Write-Host "Ignoring branch version parameter '$NugetPreReleaseParameter' -> '$NugetPreReleaseParameter'";
    }
  }
}
  
If (-not(Test-Path $nugetDir)){
  mkdir $nugetDir;
}

If (-not(Test-Path $nuget)){
  $localUrl = "\\derteamcitycache\TeamcityAgent\Nuget\Nuget.exe";
  $url = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe";
  
  If (Test-Path $localUrl) {
    Write-Host "Getting nuget.exe from local source";
    Copy-Item $localUrl $nuget;
  }
  Else {
    Write-Host "Getting nuget.exe from remote source";
    $webclient = New-Object System.Net.WebClient;
    $webclient.DownloadFile($url,$nuget);
  }
}

If (-not(Test-Path $nuget)) {
  Write-Error "NUGET EXE FAILED TO DOWNLOAD!!!!";
  Exit 10; # Just throwing an exit code to stop TeamCity from continuing.
}

Write-Host ("We are using Nuget v" + [System.Diagnostics.FileVersionInfo]::GetVersionInfo($nuget).FileVersion);

$isGIT = $false;
$isSVN = $false;

If (Test-Path .git) {  
  If (Get-Command 'git' -errorAction SilentlyContinue) {
    $isGIT = $true;
    
    # JUST CHECK THAT THIS IS NOT BOOTSTRAP CLONE
    $originURL = (git config --get remote.origin.url);
    
    If ($originURL -eq 'git@digit.mgsops.net:configurationteam/buildtemplate.git') {
      Write-Warning "RUNNING BUILD UNDER BOOTSTRAP CLONE! IGNORING BUILD UPDATE!";
      $AutoUpdateBootstrap = $false;
    }
  }
}
ElseIf (Test-Path .svn) {
  $isSVN = $true;
}

# Auto update bootstrap
If ($AutoUpdateBootstrap) {
  Write-Warning "Automatic bootstrap updater has been enabled.";
  
  If (Test-Path $derbsNugetPackageName) {
    Remove-Item $derbsNugetPackageName -Recurse;
  }
  $latestBootstrapVersion = Get-LatestBootstrapVersion;
  If (-not(Test-Path $bootstrapVersionFilename)) {
    Set-Content $bootstrapVersionFilename '0.0.0.0';
  }
  $currentBootstrapVersion = (Get-Content $bootstrapVersionFilename);
  
  If (Get-ShouldUpdateBootstrap) {
    Invoke-Expression ". `"$nuget`" Install $($derbsNugetPackageName) -ExcludeVersion -Source $nugetLibrarySource";
    
    Sleep -Seconds 1; # Due to Windows delays, we need to make sure the folder above was created and the handle released before moving it

    Copy-Item "$($derbsNugetPackageName)\bootstrap.ps1" "bootstrap.ps1" -Force;
    Copy-Item "$($derbsNugetPackageName)\build.bat" "build.bat" -Force;
    Copy-Item "$($derbsNugetPackageName)\SetupBuild.bat" "SetupBuild.bat" -Force;
    Copy-Item "$($derbsNugetPackageName)\SetupBuild.ps1" "SetupBuild.ps1" -Force;

    $addedNugetToolsInclude = $false;
    If (-not(Test-Path $customNugetToolsFile)) {
      $addedNugetToolsInclude = $true;
      Copy-Item "$($derbsNugetPackageName)\$customNugetToolsFile" $customNugetToolsFile -Force;
    }

    If (Test-Path "build.Config.proj") { # Only projects with config require this bat
      Copy-Item "$($derbsNugetPackageName)\build.Config.bat" "build.Config.bat" -Force;
    }

    Set-Content $bootstrapVersionFilename $latestBootstrapVersion;
    
    Write-Host "==============================================";
    Write-Warning ("Build process automatically updated from $currentBootstrapVersion to $latestBootstrapVersion");
    Write-Host "==============================================";
    
    If ($BranchIsDefault -eq '' -and # only will happen on users desktops - will be true/false/svn
        -not(Test-Path "M:\\BuildAgent")) { #Be super sure that it won't run on TeamCity by checking if a TC agent is installed
      # try commit
      Try {
        Write-Host $greatNewLine;
        Write-Host "==============================================";
        Write-Host "I have automatically updated the build config in your local checkout.";
        Write-Host "Here are the files that I have Updated:";

        If ($isGIT) {
          $gitstatus = (git status | Out-String);
          $gitstatusUntracked = ($gitstatus -split 'Untracked files:')[1];
          $filesICareAboutToBeCommitted = @();
          
          ForEach ($file in $filesRelatedToBuildThatCanBeAutoCommitted) {
            If ($gitstatus -match $file) {
              Write-Host "             : $($file)";
              $filesICareAboutToBeCommitted += @(
                @{ 
                  Filename = $file;
                  UnTracked = $gitstatusUntracked -match $file;
                }
              );
            }
          }

          If ($addedNugetToolsInclude -and $gitstatus -match $customNugetToolsFile) {
            Write-Host "             : $customNugetToolsFile";
            $filesICareAboutToBeCommitted += @(
                @{ 
                  Filename = $customNugetToolsFile;
                  UnTracked = $gitstatusUntracked -match $customNugetToolsFile;
                }
              );
          }
          
          If ($filesICareAboutToBeCommitted.length -eq 0) { 
            Write-Warning "No files changed after build update.";
            break;
          }
          
          Write-Host $greatNewLine;
          Write-Warning "Can I commit MY changes only?";
          $resp = (Read-Host "(Y/N)");
          If ($resp -contains 'Y') {
            ForEach ($untrackedFile in ($filesICareAboutToBeCommitted.Where({$_.UnTracked}))) {
              Write-Host " ^ Adding $($untrackedFile.Filename)";
              git add ($untrackedFile.Filename);
            }

            $filesInStringToBeCommitted = '';
            ForEach ($untrackedFile in $filesICareAboutToBeCommitted) {
              $filesInStringToBeCommitted += ' ' + $untrackedFile.Filename;
            }

            Invoke-Expression ". git commit $($filesInStringToBeCommitted) -m '[BUILD] Automatic build process update.'";
            Write-Warning 'Committed the changes. :)';
          }
          Else {
            Write-Warning "I will NOT commit any changes.";
          }
        }
        ElseIf ($isSVN) {
          $svnstatus = (svn status | Out-String);
          $filesICareAboutToBeCommitted = @();

          ForEach ($file in $filesRelatedToBuildThatCanBeAutoCommitted) {
            If ($svnstatus -match $file) {
              Write-Host "             : $($file)";
              $filesICareAboutToBeCommitted += @(
                @{ 
                  Filename = $file;
                  UnTracked = $svnstatus -match "\?([ ]+)$($file)";
                }
              );
            }
          }

          If ($addedNugetToolsInclude -and $svnstatus -match $customNugetToolsFile) {
            Write-Host "             : $customNugetToolsFile";
            $filesICareAboutToBeCommitted += @(
                @{ 
                  Filename = $customNugetToolsFile;
                  UnTracked = $svnstatus -match "\?([ ]+)$($customNugetToolsFile)";
                }
              );
          }
          
          If ($filesICareAboutToBeCommitted.length -eq 0) { 
            Write-Warning "No files changed after build update.";
            break;
          }
          
          Write-Host $greatNewLine;
          Write-Warning "Can I commit MY changes only?";
          $resp = (Read-Host "(Y/N)");
          If ($resp -contains 'Y') {
            ForEach ($untrackedFile in ($filesICareAboutToBeCommitted.Where({$_.UnTracked}))) {
              Write-Host " ^ Adding $($untrackedFile.Filename)";
              svn add "$($untrackedFile.Filename)" --non-interactive;
            }

            $filesInStringToBeCommitted = '';
            ForEach ($untrackedFile in $filesICareAboutToBeCommitted) {
              $filesInStringToBeCommitted += ' ' + $untrackedFile.Filename;
            }

            Invoke-Expression ". svn commit $($filesInStringToBeCommitted) -m `"[BUILD] Automatic build process update.`" --non-interactive";
            Write-Warning 'Committed the changes. :)';
          }
          Else {
            Write-Warning "I will NOT commit any changes.";
          }
        }
        Else {
          Write-Warning "Not a local checkout.";
        }
      }
      catch {
        Write-Warning "I tried to commit but couldn't so I am just going to continue.";
      }
    }
    
    If (Test-Path $derbsNugetPackageName) {
      Remove-Item $derbsNugetPackageName -Recurse;
    }
    
    If ($CustomRunCount -ge 3) {
      Write-Error "Recursive loop found!!!!!!!!!!!!!!!!!";
      Exit 27;
    }
    
    Write-Warning "Running new bootstrap!";
    $CustomRunCount++;
    
    Write-Warning "Removing build related files/folders before updating bootstrap.";
    
    $foldersToCleanup = @('Config', 'tools', 'dist', 'build', 'logs', 'BuildLogs', 'ConfigBuildLogs');

    ForEach ($folder in $foldersToCleanup) {
      If (Test-Path $folder) {
        Remove-Item $folder -Recurse;
      }
    }

    .\bootstrap.ps1 -Branch $Branch -BranchIsDefault $BranchIsDefault -NugetPreReleaseParameter $NugetPreReleaseParameter -CustomRunCount $CustomRunCount;
    Write-Warning "Ran new bootstrap!";
    return;
  }
}

#Restore Config Folder
If ($standBuildProcess -eq $true)
{
  If (-not(Test-Path ".\Config")) {
    Install-Tool -ID "System.Utility.DERBS" -Version $($SystemUtilityDERBSVersion) -InstallToFolderName "Config" -InstallDir $currentDir -Source $nugetLibrarySource;
  }
}
else
{
  # If we are using a custom build process Example : Mobile.MobileServices.PlayerService.BuildProcess 
  Invoke-Expression ". `"$nuget`" Install packages.config -ExcludeVersion -OutputDirectory $configDir"
  $configPackagePath = ((ls $configDir) | Where {$_.Name -like "*BuildProcess"}).PSPath
  if (Test-Path $configPackagePath)
  {
    copy-item $configPackagePath\* $configDir -recurse -force
  }
}

#Restore Tools

$CustomNugetToolsXML = $null;
$SmartToolsInstaller = $true;
$OverrideNugetToolsInstall = $false;
$fullCustomNugetToolsFile = Join-Path $currentDir $customNugetToolsFile;

If (Test-Path $fullCustomNugetToolsFile) {
  $CustomNugetToolsXML = [XML](Get-Content $fullCustomNugetToolsFile);
  $SmartToolsInstaller = ($CustomNugetToolsXML.NugetTools.SmartToolsInstaller -eq 'true');
  $OverrideNugetToolsInstall = ($CustomNugetToolsXML.NugetTools.OverrideNugetToolsInstall -eq 'true');
}

$BuildTargetsFile = "Build.targets";
$TargetsXML = $null;
$TargetsText = $null;

$fullBuildTargetsFile = (Join-Path $currentDir $BuildTargetsFile);

If ($SmartToolsInstaller) {
  Write-Warning "Smart Nuget Tools Enabled";

  If (-not(Test-Path $fullBuildTargetsFile)) {
    Write-Warning "Can't find $($BuildTargetsFile) so searching for renamed versions of it...";
    $targetFiles = Get-ChildItem -Filter '*.Build.targets' -Path  $currentDir;
    If ($targetFiles.Count -eq 1) {
      Write-Warning "Found $($targetFiles[0])";
      $BuildTargetsFile = $targetFiles[0];
      $fullBuildTargetsFile = $targetFiles[0];
    } Else {
      Write-Warning "Couldn't Find any or found too many [$($targetFiles.Count)]";
    }
  }
  
  If (Test-Path $fullBuildTargetsFile) {
    Write-Host "Running smart Nuget Tools check on $($BuildTargetsFile)";
    $TargetsText = (Get-Content $fullBuildTargetsFile);
    $TargetsXML = [XML]($TargetsText);
    If ($TargetsXML.Project.xmlns -eq $null) {
      # This doesn't seem to be a valid targets file, so for safety, lets mark it as $null to install all tools
      $TargetsXML = $null;
      Write-Warning "Invalid Targets Operation, Defaulting to install all tools.";
    } Else {
      Write-Warning "We have Smart Nuget Tools Identification And Install enabled so we are going to try see what we need as tools and then install them. If this causes any issues, disable this by setting SmartToolsInstaller=false in NugetTools.include file";
    }
  }
}

If (-not($OverrideNugetToolsInstall)) {  
  If ((Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
      -PropertyName 'Project' `
      -SearchKeys @("Maven.Build.targets", "Multi.Package.targets", "Standard.Release.Targets", "Standard.Package.targets")) -OR
      ($TargetsText -match '(.*)7Zip\\(.*)')) {
    Install-Tool `
        -ID "System.Utility.7Zip" `
        -Version "4.66" `
        -InstallToFolderName "7Zip" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource;
  }
  If ((Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
      -PropertyName 'Project' `
      -SearchKeys @("Maven.Build.targets")) -OR
      ($TargetsText -match '(.*)SysInternals\\(.*)')) {
    Install-Tool `
        -ID "System.Utility.SysInternals" `
        -Version "1.0.0.0" `
        -InstallToFolderName "SysInternals" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource;
  }
  If ((Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
      -PropertyName 'Project' `
      -SearchKeys @("Chutzpah.Test.targets", "Coverage.Report.targets", "Flash.Test.targets", "NUnit.Test.targets", "VS.Test.targets", "NUnit3.Test.targets")) -OR
      ($TargetsText -match '(.*)ReportGenerator\\(.*)')) {
    Install-Tool `
        -ID "System.Utility.ReportGenerator" `
        -Version "1.9.2.0" `
        -InstallToFolderName "ReportGenerator" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource;
  }
  If ((Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
      -PropertyName 'Project' `
      -SearchKeys @("Chutzpah.Test.targets")) -OR
      ($TargetsText -match '(.*)Chutzpah\\(.*)')) {
    Install-Tool `
        -ID "System.Utility.Chutzpah" `
        -Version "3.2.3.0" `
        -InstallToFolderName "Chutzpah" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource `
        -ExcludeVersion $true;
  }
  If ((Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
      -PropertyName 'Project' `
      -SearchKeys @("Flash.Test.targets", "Microsoft.Test.targets", "NUnit.Test.targets", "NUnit3.Test.targets")) -OR
      ($TargetsText -match '(.*)OpenCover\\(.*)')) {
    Install-Tool `
        -ID "System.Utility.OpenCover" `
        -Version "4.0.519" `
        -InstallToFolderName "OpenCover" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource;
  }
  If ((Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
      -PropertyName 'Project' `
      -SearchKeys @("Google.Test.Targets", "GTest.VSProfiler.Test.Targets", "MPV.FlashClient.Flex.Test.targets", "NCover.Test.Targets", "NUnit.Test.targets", "NUnit3.Test.targets")) -OR
      ($TargetsText -match '(.*)NUnit\\(.*)')) {
    Install-Tool `
        -ID "System.Utility.NUnit" `
        -Version "2.6.4.14350" `
        -InstallToFolderName "NUnit" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource;
  }
  If ((Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
    -PropertyName 'Project' `
    -SearchKeys @("NUnit3.Test.targets")) -OR
    ($TargetsText -match '(.*)NUnit3\\(.*)')) {
  Install-Tool `
      -ID "System.Utility.NUnit" `
      -Version "3.10.1.9" `
      -InstallToFolderName "NUnit3" `
      -InstallDir $toolsDir `
      -Source $nugetLibrarySource;
  }
  If ((Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
      -PropertyName 'Project' `
      -SearchKeys @("Google.Test.Targets", "GTest.VSProfiler.Test.Targets")) -OR
      ($TargetsText -match '(.*)gtest\\(.*)')) {
    Install-Tool `
        -ID "System.Utility.gtest" `
        -Version "1.0.0.0" `
        -InstallToFolderName "gtest" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource;
  }
  If (Tool-Required -XmlNodesToValidateAgainst $TargetsXML.Project.Import `
      -PropertyName 'Project' `
      -SearchKeys @("Docker.Build.targets")) {
    Install-Tool `
        -ID "System.Utility.DERBS.MSBuild" `
        -Version "15.0.2" `
        -InstallToFolderName "System.Utility.DERBS.MSBuild" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource;
  }
  If ($TargetsText -match '(.*)FlashDevelop\\(.*)') {
    Install-Tool `
        -ID "System.Utility.FlashDevelop" `
        -Version "1.0.0.2" `
        -InstallToFolderName "FlashDevelop" `
        -InstallDir $toolsDir `
        -Source $nugetLibrarySource;
  }
}

If ($CustomNugetToolsXML -ne $null) {
  ForEach ($nugetTool in $CustomNugetToolsXML.NugetTools.NugetTool) {
    Install-Tool `
      -ID ($nugetTool.Id) `
      -Version ($nugetTool.Version) `
      -Source ($nugetTool.Source) `
      -InstallDir ($nugetTool.InstallDir) `
      -InstallToFolderName ($nugetTool.CustomDirName) `
      -ExcludeVersion ($nugetTool.ExcludeVersion -ne 'false') `
      -ExtraCommandParameters ($nugetTool.ExtraCommandParameters) `
      -Comment ($nugetTool.Comment)
  }
}

$stopWatch.Stop();
Write-Warning "Operation took $($stopWatch.get_Elapsed())";

If ($Error.Count -gt 0) {
  # If there were any issues that have occured, i.e.: Nuget install failing, we will pick it up here
  Write-Warning "An error occured during the execution of this script! Below is a copy of the errors but scroll up to find the exact error";
  Write-Output $Error;
  Exit 24;
}