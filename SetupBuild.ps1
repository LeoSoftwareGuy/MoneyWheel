$Locations = @();
$maxParentFolderCommonAssemblyInfoDirectoriesToCheck = 4;
$maxParentFolderBuildProjDirectoriesToCheck = 8;
$buildBatUrl = $null;
$parametersForBuild = "/t:All /p:Configuration=Release";



function Find-Pattern {
param([string]$LineMatch = $(Throw "you must specify a pattern to check for"),
      [string[]]$ProjLines = $(Throw "you must specify a pattern to check against")          
)  

    ForEach ($LineItem in $ProjLines){ 
        If ($LineItem.Contains($LineMatch)) {
            return $true;
        }
    }    
    return $false;
}

Write-Host "Hey there! ";
Write-Host "Please paste the absolute path to the projects you want to update.";
Write-host "(ps, you can paste a repo path and we will find the csproj files)";
Write-host "";

do {
  $path = Read-Host "Absolute Project Folder Path$([Environment]::NewLine)Or Repo Folder Path (.\) $([Environment]::NewLine)Or press enter to start setup";
  $filesInDir = (Get-ChildItem $path -r -Include *.csproj, *.vcxproj, *.sqlproj )#-Exclude "*test*");
  
  If ($filesInDir.length -eq 0) {
    Write-Host "Woah there! There is no csproj/vcxproj/sqlproj files in that location!";
    continue;
  }
  
  ForEach ($file in $filesInDir) {
    $locat = $file.VersionInfo.FileName.replace($file.Name, "");
    Write-Host "Okay, found a base location '$locat'";
    If (-not($Locations.contains($locat))) {
        $Locations += @($locat);
    }
  }
}
While ($path -ne '');

cls;

Write-Host "The following locations are going to be setup ("$Locations.length")";
Write-Output $Locations;

If ((Read-Host "Please confirm this ? (Y/N)") -ne "Y") {
    Write-Host "Aborted!";
    return;
}

If ($Locations.length -eq 0) {
  Write-Host "Woah there! You haven't give us any paths!";
  return;
}

Write-Host " -- Started";

$nl = [System.Environment]::NewLine

$WebProjects = @()
$LibProjects = @()
$csprojs = @()
$vcxprojs = @()
$propFiles = @()

ForEach ($Location in $Locations)
{
  $propFiles += @(Get-ChildItem $Location -r -Include AssemblyInfo.cs -Exclude "**\*test*\**");
  $csprojs += @(Get-ChildItem $Location -r -Include *.csproj )#-Exclude "*test*");
  $csprojs += @(Get-ChildItem $Location -r -Include *.sqlproj )#-Exclude "*test*");
  $vcxprojs += @(Get-ChildItem $Location -r -Include *.vcxproj )#-Exclude "*test*");
}

Write-Host "Found the following csproj/sqlproj files:";
Write-Output $csprojs;

$pattern = '<Compile Include="Properties\\AssemblyInfo.cs" />';
$commonAssemblyExistsPattern = '<Link>Properties\CommonAssemblyInfo.cs</Link>';
$replacement = '<Compile Include="Properties\AssemblyInfo.cs" /> ' + $nl;
$replacement += '    <Compile Include="%backRef%CommonAssemblyInfo.g.cs"> ' + $nl;
$replacement += '      <Link>Properties\CommonAssemblyInfo.cs</Link>' + $nl;
$replacement += '    </Compile>'

ForEach ($csproj in $csprojs) {
    Write-Host "[Versioning] Working on '$csproj'";

    $NewLines = @()
    $Lines = Get-Content $csproj.FullName;
 
    $commonAssemblyExists = Find-Pattern -LineMatch $commonAssemblyExistsPattern -ProjLines $Lines;
    $isDotNetStd = Find-Pattern -LineMatch '<TargetFramework>' -ProjLines $Lines;
     
    If ($commonAssemblyExists -Or $isDotNetStd) {
        Write-Host "'$csproj' does not require common assembly info reference - ignoring";
        continue;
    }

    $backReference = '';
    $directoryName = (dir $csproj).VersionInfo.FileName.replace($csproj.Name, "");

    For ($i = 0; $i -lt $maxParentFolderCommonAssemblyInfoDirectoriesToCheck ; $i++) {
        If (Test-Path (Join-Path $directoryName ($backReference+"CommonAssemblyInfo.g.cs"))) {
            break;
        }

        $backReference += "..\";
    }

    If (-not(Test-Path (Join-Path $directoryName ($backReference+"CommonAssemblyInfo.g.cs")))) {
        Write-Host "'$csproj' We couldn't find a CommonAssemblyInfo.g.cs file within the top $maxParentFolderCommonAssemblyInfoDirectoriesToCheck parent folders, so we are ignoring versioning";
        continue;
    }
    
    If ($buildBatUrl -eq $null) {
      $buildPath = (Join-Path $directoryName ($backReference+"..\build.bat"));
      If (Test-Path $buildPath) {
        $buildBatUrl = $buildPath;
      }
    }

    $finishedReplacement = $replacement.Replace('%backRef%', $backReference)

    ForEach ($Line in $Lines){ 
        $NewLines += $Line -replace $pattern, $finishedReplacement;
    } 
    Set-Content $csproj $NewLines -Force;
 }

ForEach ($csproj in $csprojs) {
  $isWebProject = 0;
  $Lines = Get-Content $csproj.FullName
  $isWebProject = Find-Pattern -LineMatch 'Microsoft.WebApplication.targets' -ProjLines $Lines;
  $isDotNetStd = Find-Pattern -LineMatch '<TargetFramework>' -ProjLines $Lines;

  if ($isWebProject){
     $WebProjects += $csproj.FullName
     }
  elseif ($isDotNetStd) {
    continue;
  }
  else {
     $LibProjects += $csproj.FullName
  }
}
    
$notOkayPattern = '<TargetArtifacts Include="@(Content);$(OutputPath)**" />';
$WebSiteCopy = '  <PropertyGroup>' + $nl;
$WebSiteCopy += '    <SiteName Condition="''$(SiteName)'' == ''''">$(AssemblyName)</SiteName>' + $nl;
$WebSiteCopy += '    <WebProjectOutputDir>$(BuildDir)$(SiteName)\</WebProjectOutputDir>' + $nl;
$WebSiteCopy += '  </PropertyGroup>' + $nl;


$WebSiteCopy += '  <PropertyGroup>' + $nl;
$WebSiteCopy += '    <WorkingDir Condition="''$(WorkingDir)'' == ''''">$(MSBuildProjectDirectory)\%backRef%</WorkingDir>' + $nl;
$WebSiteCopy += '    <BuildDir Condition="''$(BuildDir)'' == ''''">$(WorkingDir)build\</BuildDir>' + $nl;
$WebSiteCopy += '  </PropertyGroup>' + $nl;
$WebSiteCopy += '  <Target Name="ValidateBuildProperties">' + $nl;
$WebSiteCopy += '    <Error Text="The WorkingDir property is not defined."' + $nl;
$WebSiteCopy += '      Condition="''$(WorkingDir)'' == ''''" />' + $nl;
$WebSiteCopy += '    <Error Text="The WorkingDir must have a trailing slash."' + $nl;
$WebSiteCopy += '      Condition="!HasTrailingSlash(''$(WorkingDir)'')" />' + $nl;
$WebSiteCopy += '    <Error Text="The BuildDir property is not defined."' + $nl;
$WebSiteCopy += '      Condition="''$(BuildDir)'' == ''''" />' + $nl;
$WebSiteCopy += '    <Error Text="The BuildDir must have a trailing slash."' + $nl;
$WebSiteCopy += '      Condition="!HasTrailingSlash(''$(BuildDir)'')" />' + $nl;    
$WebSiteCopy += '  </Target>' + $nl

$WebSiteCopy += '  <Target Name="AfterBuild">' + $nl
$WebSiteCopy += '    <ItemGroup>' + $nl
$WebSiteCopy += '      <TargetArtifacts Include="@(Content);$(OutputPath)**" />' + $nl
$WebSiteCopy += '    </ItemGroup>' + $nl
$WebSiteCopy += '    <Copy SourceFiles="@(TargetArtifacts)" DestinationFiles="@(TargetArtifacts->'
$WebSiteCopy += '''$(BuildDir)$(AssemblyName)\%(RelativeDir)%(Filename)%(Extension)'''
$WebSiteCopy += ')" SkipUnchangedFiles="true" />' + $nl
$WebSiteCopy += '  </Target>' + $nl
$WebSiteCopy += '</Project>'

If ($WebProjects -ne $null) {
  ForEach ($WebProject in $WebProjects) {
    Write-Host "[WebProjects] Working on '$WebProject'";
    $NewLines = @()
    $LineNumber= 1;
    $Lines = Get-Content $WebProject
    $NumberOfLinesMinusFive = $Lines.Length - 5
    
    $okayToAdd = $true;

    ForEach ($Line in $Lines){ 
        If ($line.Contains($notOkayPattern)) {
            $okayToAdd = $false;
            break;
        }
    } 

    If (-not($okayToAdd)) {
        Write-Host "'$WebProject' already has afterbuild reference - ignoring";
        continue;
    }


    $backReference = '';
    $refDir = (dir $WebProject);
    $directoryName = $refDir.VersionInfo.FileName.replace($refDir.Name, "");

    For ($i = 0; $i -lt $maxParentFolderBuildProjDirectoriesToCheck ; $i++) {
      $pathToTest = (Join-Path $directoryName ($backReference+"build.proj"));
      Write-Host "Checking back path '$pathToTest'";
        If (Test-Path $pathToTest) {
          Write-Host "Found build.proj at '$pathToTest'";
            break;
        }

        $backReference += "..\";
    }
    
    If (-not(Test-Path (Join-Path $directoryName ($backReference+"build.proj")))) {
        Write-Host "'$csproj' We couldn't find a build.proj file within the top $maxParentFolderBuildProjDirectoriesToCheck parent folders, so we are ignoring versioning";
        break;
    }

    $finishedReplacement = $WebSiteCopy.Replace('%backRef%', $backReference);

    ForEach ($Line in $Lines){ 
     if ($LineNumber -gt $NumberOfLinesMinusFive){
     $NewLines += $Line -replace '</Project>', $finishedReplacement
    }
    else {
      $NewLines += $Line
      }
    $LineNumber++;
    } 
    Set-Content $WebProject $NewLines -Force
  }
}

$notOkayPattern = 'DestinationFolder="$(BuildDir)$(AssemblyName)\%(RecursiveDir)';
$LibraryCopy = '<PropertyGroup>' + $nl;
$LibraryCopy += '    <WorkingDir Condition="''$(WorkingDir)'' == ''''">$(MSBuildProjectDirectory)\%backRef%</WorkingDir>' + $nl;
$LibraryCopy += '    <BuildDir Condition="''$(BuildDir)'' == ''''">$(WorkingDir)build\</BuildDir>' + $nl;
$LibraryCopy += '  </PropertyGroup>' + $nl;
$LibraryCopy += '  <Target Name="ValidateBuildProperties">' + $nl;
$LibraryCopy += '    <Error Text="The WorkingDir property is not defined."' + $nl;
$LibraryCopy += '      Condition="''$(WorkingDir)'' == ''''" />' + $nl;
$LibraryCopy += '    <Error Text="The WorkingDir must have a trailing slash."' + $nl;
$LibraryCopy += '      Condition="!HasTrailingSlash(''$(WorkingDir)'')" />' + $nl;
$LibraryCopy += '    <Error Text="The BuildDir property is not defined."' + $nl;
$LibraryCopy += '      Condition="''$(BuildDir)'' == ''''" />' + $nl;
$LibraryCopy += '    <Error Text="The BuildDir must have a trailing slash."' + $nl;
$LibraryCopy += '      Condition="!HasTrailingSlash(''$(BuildDir)'')" />' + $nl; 
$LibraryCopy += '  </Target>' + $nl

$LibraryCopy += '  <Target Name="AfterBuild">' + $nl
$LibraryCopy += '    <ItemGroup>' + $nl
$LibraryCopy += '      <TargetArtifacts Include="$(TargetDir)**\*.*" />' + $nl
$LibraryCopy += '      <TargetArtifacts Remove="$(TargetDir)*.vshost.exe*" />' + $nl
$LibraryCopy += '      <TargetArtifacts Remove="$(TargetPath).*.xml" />' + $nl
$LibraryCopy += '      <TargetArtifacts Remove="$(TargetPath).lastcodeanalysissucceeded" />' + $nl
$LibraryCopy += '    </ItemGroup>' + $nl
$LibraryCopy += '    <Copy SourceFiles="@(TargetArtifacts)"' + $nl
$LibraryCopy += '          DestinationFolder="$(BuildDir)$(AssemblyName)\%(RecursiveDir)"' + $nl
$LibraryCopy += '          SkipUnchangedFiles="true" />' + $nl
$LibraryCopy += '  </Target>' + $nl
$LibraryCopy += '</Project>'

If ($LibProjects -ne $null) {
  ForEach ($LibProject in $LibProjects) {
    Write-Host "[LibProject] Working on '$LibProject'";
    $NewLines = @()
    $LineNumber= 1;
    $Lines = Get-Content $LibProject
    $NumberOfLinesMinusFive = $Lines.Length - 5
    
    $okayToAdd = $true;

    ForEach ($Line in $Lines){ 
        If ($line.Contains($notOkayPattern)) {
            $okayToAdd = $false;
            break;
        }
    } 

    If (-not($okayToAdd)) {
        Write-Host "'$LibProject' already has afterbuild info reference - ignoring";
        continue;
    }


    $backReference = '';
    $refDir = (dir $LibProject);
    $directoryName = $refDir.VersionInfo.FileName.replace($refDir.Name, "");

    For ($i = 0; $i -lt $maxParentFolderBuildProjDirectoriesToCheck ; $i++) {
      $pathToTest = (Join-Path $directoryName ($backReference+"build.proj"));
      Write-Host "Checking back path '$pathToTest'";
        If (Test-Path $pathToTest) {
          Write-Host "Found build.proj at '$pathToTest'";
            break;
        }

        $backReference += "..\";
    }

    If (-not(Test-Path (Join-Path $directoryName ($backReference+"build.proj")))) {
        Write-Host "'$csproj' We couldn't find a build.proj file within the top $maxParentFolderBuildProjDirectoriesToCheck parent folders, so we are ignoring versioning";
        break;
    }

    $finishedReplacement = $LibraryCopy.Replace('%backRef%', $backReference);

    ForEach ($Line in $Lines){ 
     if ($LineNumber -gt $NumberOfLinesMinusFive){
     $NewLines += $Line -replace '</Project>', $finishedReplacement
    }
    else {
      $NewLines += $Line
      }
    $LineNumber++;
    } 
    Set-Content $LibProject $NewLines -Force
  }
}
     
ForEach ($propFile in $propFiles) {
  $NewText = @()
  $Lines = Get-Content $propFile.FullName
  $changes = $false;
  Foreach ($Line in $Lines){
      if (-not($Line.StartsWith('//'))) {
          if ($Line -match 'AssemblyProduct'){
              $NewText += '//' + $Line;
              $changes = $true;
          }
          elseif ($Line -match 'AssemblyCopyright'){
              $NewText += '//' + $Line;
              $changes = $true;
          }
          elseif ($Line -match 'AssemblyCompany'){
              $NewText += '//' + $Line;
              $changes = $true;
          }
          elseif ($Line -match 'AssemblyVersion'){
              $NewText += '//' + $Line;
              $changes = $true;
          }
          elseif ($Line -match 'AssemblyFileVersion'){
              $NewText += '//' + $Line;
              $changes = $true;
          }
          else {
              $NewText += $Line
          }
      }
      else {
          $NewText += $Line
      }
  }
  If ($changes) {
      Remove-Item $propFile.FullName -Force
      Set-Content $propFile.FullName $NewText
  }
}

Write-Host "Found the following vcxproj files:";
Write-Output $vcxprojs;

$pattern = '  <Import Project="%backRef%Config\AfterBuild.Cpp.targets" />';
$notOkayPattern = '\Config\AfterBuild.Cpp.targets';

ForEach ($vcxproj in $vcxprojs) {
  Write-Host "[Versioning] Working on '$vcxproj'";

  $NewLines = @()
  $Lines = Get-Content $vcxproj.FullName;

  $okayToAdd = $true;

  ForEach ($Line in $Lines){ 
      If ($line.Contains($notOkayPattern)) {
          $okayToAdd = $false;
          break;
      }
  } 

  If (-not($okayToAdd)) {
      Write-Host "'$vcxproj' already has the after targets reference - ignoring";
      break;
  }

  $backReference = '';
  $directoryName = (dir $vcxproj).VersionInfo.FileName.replace($vcxproj.Name, "");

  For ($i = 0; $i -lt $maxParentFolderBuildProjDirectoriesToCheck ; $i++) {
      $pathToTest = (Join-Path $directoryName ($backReference+"build.proj"));
      Write-Host "Checking back path '$pathToTest'";
        If (Test-Path $pathToTest) {
          Write-Host "Found build.proj at '$pathToTest'";
          break;
      }

      $backReference += "..\";
  }

  If (-not(Test-Path (Join-Path $directoryName ($backReference+"build.proj")))) {
      Write-Host "'$vcxproj' We couldn't find a build.proj file within the top $maxParentFolderBuildProjDirectoriesToCheck parent folders, so we are ignoring versioning";
      break;
  }

  $finishedReplacement = $pattern.Replace('%backRef%', $backReference)

  ForEach ($Line in $Lines){ 
    If ($Line.Contains("</Project>")) {
      $NewLines += $finishedReplacement;
    }
    
    $NewLines += $Line;
  } 
  Set-Content $vcxproj $NewLines -Force;
}  
    
Write-Host " -- Completed";

If ($buildBatUrl -eq $null) {
  Return;
}

Write-Host ([Environment]::NewLine + [Environment]::NewLine);
Write-Host "Build I will run: $($buildBatUrl)";
Write-Host "Build parameters: $($parametersForBuild)";
Write-Host ([Environment]::NewLine + [Environment]::NewLine);
If ((Read-Host "Can I run the build to push the version info to the projects ? (Y/N)") -eq "Y") {
  & cmd.exe /c "$($buildBatUrl) $($parametersForBuild)";
}