# Configuration Management (Continuous Integration / Version Control)

* * *

## Setting up a new build/project

> Requirements:
> 
> *   You must already have an **_empty_** repo created on either SVN/GIT/TFS  
>     _This means there must be no build files related to our build process in your repo_
>     
>     
> *   You must checkout/clone the repo onto your machine so it is ready to work with
>
> 
> Time Management: 
> 
> * It should take around 20-30 minutes to do the whole setup if you have never done it before 
> 
> * Once you have done it a few times, it should take around 10-20 minutes 

### 1. Use the following directory structure for your project

> Notes:
> 
> *   If you already have committed code, you will need to refactor your structure to match the following:
> *   Filenames used here are examples
> *   The **_lib_** directory must only contain libraries that cannot be installed via nuget packages or some form of package manager within your IDE

> `
> ├ src/  
> │ ├ *.sln  
> │ ├ ProjectName/  
> │ > ├ *.*proj  
> │ ├ GulpProjectName/  
> │ > ├ gulp.js  
> │ ├ NPMProjectName/  
> │ > ├ app.js  
> ├ lib/  
> │ ├ LibName/  
> │ > ├ *.dll
> `

*   Create the following directories in your repo:
    *   **_src_** (in the root of your repo)
    *   **_lib_** (in the root of your repo)
*   Create as many directories in the src/ dir according to the projects you are going to add to this repo

### 2. Add your project source code into the relevant project directories (within the project dir, within the src dir)

> Notes:
> 
> *   You can skip this step if you already have a repo setup with the above directory structure
> *   The .sln (Visual Studio Solution File) must be added directly into the src/ dir

### 3. Make sure your project builds locally in its current state

> Notes:
> 
> *   Open your project in your IDE of choice (If you build using an IDE)
> *   Run your build and make sure it succeeds
> *   Do not continue until you have a successful build

### 4. Update your version control ignore list

> Notes:
> 
> Add relevant binary/generated files to your ignore list  
> Add the following as well:
> 
> *   /[Bb]uild
> *   /[Cc]onfig
> *   /[Dd]ist
> *   /[Ll]ogs
> *   /[Tt]ools
> *   /[Bb]uild[Ll]ogs
> *   [Pp]ackages/
> *   /*.log

### 5. Cleanup all generated files from the build

> Notes:
> 
> *   Close your IDE before deleting any files
> 
> What to delete?
> 
> *   bin, obj directories
> *   *.user, *.suo files

### 6. Commit your now working code

> Notes:
> 
> *   git commit -m "I love GIT and my code works!"
> *   svn commit -m "I want to move to GIT but my code works!"

### 7. Bootstrap your project

##### 7.1. Go to [System.Utility.DERBS.Bootstrap](https://fw.minion/cfgbooty) and download the latest nuget package.

##### 7.2. Once downloaded, open the package using [7-Zip](http://www.7-zip.org/download.html) (Or any compatible zip archive software)

##### 7.3. Copy over the following files into your project from the bootstrap: (from **_->_** to)

> Notes:
> 
> *   Quick file explanations:
>     
>     
>     *   `bootstrap.ps1`  
>         _Handles automatic updating of the build system within your project so that we can make bugfixes that propogate automatically_  
>         _Pulls down our config directory which holds all of our build targets to defined how to actually build your project(s)_  
>         _Pulls down tools required to build your project(s) or for tests and reporting_  
>         _Pulls down any additional tools defined in the NugetTools.Include_
>     *   `build.proj`  
>         _Stores your build definitions_  
>         _This file defines what and how your projects must be built, versioned or tested_
>     *   `Build.Targets`  
>         _Defines all referenced .targets that the build must load to for your build_
>     *   `SetupBuid.bat`  
>         _Sets your build up by adding references in the *.proj files_  
>         _References the `CommonAssemblyInfo.cs`_  
>         _Adds an `AfterBuild` step to copy the built artifacts to the `/Build` directory_

`├ _src/ -> ├ src/  
│ ├ CommonAssemblyInfo.g.cs -> │ ├ CommonAssemblyInfo.g.cs  ├ Bootstrap.ps1 -> ├ bootstrap.ps1  
├ Build.bat -> ├ Build.bat  
├ Build.proj -> ├ Build.proj  
├ Build.targets -> ├ Build.targets  
├ NugetTools.include -> ├ NugetTools.include  
├ SetupBuild.bat -> ├ SetupBuild.bat  
├ SetupBuild.ps1 -> ├ SetupBuild.ps1  
├ Version.include -> ├ Version.include  
├ deploy/ -> ├ deploy/  
│ ├ DeployableNugetPackageTemplate/ -> │ ├ DeployableNugetPackageTemplate/  
│ > ├ Template.nuspec.dist -> │ > ├ Template.nuspec.dist  
│ > ├ PostDeploy.ps1 -> │ > ├ PostDeploy.ps1  
│ > ├ PreDeploy.ps1 -> │ > ├ PreDeploy.ps1  
│ > ├ DeployFailed.ps1 -> │ > ├ DeployFailed.ps1  
│ > ├ Deploy.ps1 -> │ > ├ Deploy.ps1  
│ ├ DeployableNugetPackageTemplate.Config/ -> │ ├ DeployableNugetPackageTemplate.Config/  
│ > ├ Template.Config.nuspec.dist -> │ > ├ Template.Config.nuspec.dist  
│ > ├ PostDeploy.ps1 -> │ > ├ PostDeploy.ps1  
│ > ├ PreDeploy.ps1 -> │ > ├ PreDeploy.ps1  
│ > ├ DeployFailed.ps1 -> │ > ├ DeployFailed.ps1  
│ > ├ Deploy.ps1 -> │ > ├ Deploy.ps1  
│ ├ LibraryNugetPackageTemplate/ -> │ ├ LibraryNugetPackageTemplate/  
│ > ├ Library.Template.nuspec.dist -> │ > ├ Library.Template.nuspec.dist`

### 8. Setting up our bootstrap to build your project

##### 8.1. Update `version.include` with your project's information.

> Update `<ProductName>Template</ProductName>` with your products name (_No spaces_)  
> Update `<ProductFullName>Template</ProductFullName>` with your products full name (_Can contain spaces_)

##### 8.2. Update `build.proj` to point to your projects

> Update the default project reference `<Project Include="src\Template.sln"/>` to point to your .sln file (_duplicate this line to add multiple files_)  
> If you have other types of projects, there are examples included in the build.proj  
> If you do not have a `*.sln` file, you will need to comment out the above referenced line and then go onto step _#8.3_

##### 8.3. Update `Build.targets` **if** you require special features like **NUnit** or **gtest** or **NPM** or **Gulp**

> Uncomment the reference to the `*.targets` for the required feature (Using a proper dev's text editor... like notepad... ++)  
> If you are not building any C# projects, you must comment out `<Import Project="config\Standard.Build.targets"/>`

##### 8.4. Double click on `SetupBuild.bat` within your local repo.

> Notes:
> 
> *   If you are asked to commit, say `No`
> *   If it does not open CMD, then open CMD in the current directory and call it manually `SetupBuild.bat`
> 
> Errors:
> 
> *   `SetupBuild.ps1 cannot be loaded because the execution of scripts is disabled on this system`  
>     
>     *   You will need to open **powershell** as **administrator** and run `Set-ExecutionPolicy Unrestricted`

##### 8.5. Enter the locations of the .csproj/.vsproj/.*proj's

> Commands:
> 
> *   `.\` will setup all projects within your repo
> *   _Absolute path of your projects_
> 
> Notes:
> 
> *   Pressing **enter** will **start** the **setup**
> *   Let it run a **build** but **DON'T COMMIT**. (If the build fails, there could be a config error in your setup)

### 9. Validate your projects built

> Notes:
> 
> *   Our build process will build and copy the built dlls/exes into the /Build/{ProjectAssemblyName}/ directory
> *   NPM/Gulp/Grunt projects will not be copied to the /Build directory but more than likely a /dist directory within the project dir

*   Go into the /Build/{ProjectAssemblyName}/ directory and check your dlls/exes were outputted
*   **Or** check in the dist directory within the project directory if is a NPM project

### 10. Configure NuGet packages for the publish of your project

> Notes:
> 
> *   .nuspec **vs** .nupkg
>     *   `.nuspec` - **NuGet specification file** (used to _define_ what a NuGet package will look like)
>     *   `.nupkg` - **NuGet package** (A _fancy ZIP_ file with extra information)

##### 10.1. Work out the nuget package ID's

> Notes:
> 
> In order to publish to our NuGet feeds, you will need to have the correct NuGet package Ids  
> Go to [Automated Deployments Naming Standards](https://fw.minion/atdnames) and work out what your nuget package id(s) will need to be.

##### 10.2. List (_On notepad, paper or simply remember - just for reference to make the following easier_) the NuGet package Ids and their package type.

> Package Types:
> 
> *   **Library** packages (`LibraryNugetPackageTemplate`) (_packages installed using Nuget Package Manager in VS_) go to `http://nugetlib/`
> *   **Deployable** packages (`DeployableNugetPackageTemplate`) (_packages deployed using Octopus_) go to `http://devget/`
> *   **Config** packages (`DeployableNugetPackageTemplate.Config`) (_packages containing web.config or other purely configuration related files_) go to `http://devget/` & `http://testget/` & `http://liveget/`

##### 10.3. Remove any extra nuspec package types that you do not require

> What files to delete per package type not required:
> 
> *   **Library**  
>     `├ deploy/  
>     │ ├ LibraryNugetPackageTemplate/  
>     │ > ├ Library.Template.nuspec.dist`
>     
>     
> *   **Deployable**  
>     `├ deploy/  
>     │ ├ DeployableNugetPackageTemplate/  
>     │ > ├ Template.nuspec.dist  
>     │ > ├ PostDeploy.ps1  
>     │ > ├ PreDeploy.ps1  
>     │ > ├ DeployFailed.ps1  
>     │ > ├ Deploy.ps1`
>     
>     
> *   **Config**  
>     `├ deploy/  
>     │ ├ DeployableNugetPackageTemplate.Config/  
>     │ > ├ Template.Config.nuspec.dist  
>     │ > ├ PostDeploy.ps1  
>     │ > ├ PreDeploy.ps1  
>     │ > ├ DeployFailed.ps1  
>     │ > ├ Deploy.ps1`

> Notes: 
>
> You only delete the .nuspec, .ps1 files and directory that you don't want to keep or that is unrelated to your build.
> > Eg: Delete `deploy/LibraryNugetPackageTemplate` directory (which deletes all files within) as we are deploying our project using Octopus and not pushing a library to be used in other projects

##### 10.4. Have multiple or a specific package type? (_Eg: You need 2 library nuget packages_)

> Duplicate the relevant nuget package type directory
> 
> *   EG: If you have 2 Library packages, make a copy of the `LibraryNugetPackageTemplate` directory so now you would have something like this:  
>     `├ deploy/  
>     │ ├ LibraryNugetPackageTemplate/  
>     │ > ├ Library.Template.nuspec.dist  
>     │ ├ LibraryNugetPackageTemplate - Copy/  
>     │ > ├ Library.Template.nuspec.dist`

##### 10.5. Rename the nuget package type directories

> Notes:
> 
> If your NuGet package id is: (**EG**) Internal.Web.Canvas  
> Then you would rename one of the directories from `DeployableNugetPackageTemplate` to `Internal.Web.Canvas` (_According to the package type_) Examples:
> 
> *   `LibraryNugetPackageTemplate` -> `Internal.Library.Canvas.SDK`
> *   `DeployableNugetPackageTemplate.Config` -> `Internal.Web.Canvas.Config`
> *   `DeployableNugetPackageTemplate.Config` -> `Banking.Service.PAPI.Config`
> *   `LibraryNugetPackageTemplate` -> `Banking.Library.PAPI`

##### 10.6. Rename the .nuspec.dist files

> Notes:
> 
> If your NuGet package id is: (**EG**) Internal.Web.Canvas  
> Then you would go into the relevant directory, eg: `Internal.Web.Canvas` (which was `DeployableNugetPackageTemplate`) and rename the `DeployableNugetPackageTemplate.nuspec.dist` to `Internal.Web.Canvas.nuspec` By dropping the .dist you now make it a valid nuspec file Examples:
> 
> *   `Internal.Library.Canvas.SDK/LibraryNugetPackageTemplate.nuspec.dist` -> `Internal.Library.Canvas.SDK/Internal.Library.Canvas.SDK.nuspec`
> *   `Internal.Web.Canvas.Config/DeployableNugetPackageTemplate.Config.nuspec.dist` -> `Internal.Web.Canvas.Config/Internal.Web.Canvas.Config.nuspec`
> *   `Banking.Service.PAPI.Config/DeployableNugetPackageTemplate.Config.nuspec.dist` -> `Banking.Service.PAPI.Config/Banking.Service.PAPI.Config.nuspec`
> *   `Banking.Library.PAPI/LibraryNugetPackageTemplate.nuspec.dist` -> `Banking.Library.PAPI/Banking.Library.PAPI.nuspec`

##### 10.7. Update the .nuspec files to package your projects

> Notes:
> 
> You can open the .nuspec files using a text editor (like notepad... ++)  
> .nuspec files are XML files

###### 10.7.1. Update the `<id>Template</id>` element to the relevant nuget package id.

> Examples:
> 
> `Internal.Library.Canvas.SDK` -> `<id>Template</id>` -> `<id>Internal.Library.Canvas.SDK</id>`  
> `Internal.Web.Canvas.Config` -> `<id>Template</id>` -> `<id>Internal.Web.Canvas.Config</id>`  
> `Banking.Service.PAPI.Config` -> `<id>Template</id>` -> `<id>Banking.Service.PAPI.Config</id>`  
> `Banking.Library.PAPI` -> `<id>Template</id>` -> `<id>Banking.Library.PAPI</id>`

###### 10.7.2 Update the `<file src="..\deploy\Template\*.ps1" />` (***.ps1 reference only**) file reference element with the relevant nuget package id.

> Examples:
> 
> `Internal.Library.Canvas.SDK` -> `<file src="..\deploy\Template\*.ps1" />` -> `<file src="..\deploy\Internal.Library.Canvas.SDK\*.ps1" />`  
> `Internal.Web.Canvas.Config` -> `<file src="..\deploy\Template\*.ps1" />` -> `<file src="..\deploy\Internal.Web.Canvas.Config\*.ps1" />`  
> `Banking.Service.PAPI.Config` -> `<file src="..\deploy\Template\*.ps1" />` -> `<file src="..\deploy\Banking.Service.PAPI.Config\*.ps1" />`  
> `Banking.Library.PAPI` -> `<file src="..\deploy\Template\*.ps1" />` -> `<file src="..\deploy\Banking.Library.PAPI\*.ps1" />`

###### 10.7.3 Update the other file reference elements with the relevant nuget package id.

*   Library package type

    > Notes:
    > 
    > Library packages should only contain the single DLL and/or pdb of your project and can contain other config/content related files that are required by your project  
    > All other dlls/pdbs should **NOT** be referenced as they should be dependencies of this nuget package

    *   Update the `<file src="Template\MyLibrary.dll" target="lib\net4" />` reference to point to the dll from your project  

        > Notes:  
        > 
        > The file references are all relative to /Build directory  
        > You can use wild card references  
        >   
        > Examples:  
        >   
        > 
        > *   Single DLL: `Internal.Library.Canvas.SDK` -> `<file src="Template\MyLibrary.dll" target="lib\net4" />` -> `<file src="CanvasSDK\CanvasSDK.dll" target="lib\net452" />`  
        >     `Banking.Library.PAPI` -> `<file src="Template\MyLibrary.dll" target="lib\net4" />` -> `<file src="PAPI\PAPI.dll" target="lib\net2" />`  
        >     
        >     
        >     
        > *   DLL with PDB: `Internal.Library.Canvas.SDK` -> `<file src="Template\MyLibrary.dll" target="lib\net4" />` -> `<file src="CanvasSDK\CanvasSDK.*" target="lib\net452" />`  
        >     `Banking.Library.PAPI` -> `<file src="Template\MyLibrary.dll" target="lib\net4" />` -> `<file src="PAPI\PAPI.*" target="lib\net2" />`  
        >       
        >     By using a wildcard for the file extension we include both the .dll and .pdb files  
        >       
        >     _The assembly info does not need to match the nuget package id as the nuget package id is only used during the CI/Deployment pipeline_  
        >     
        >     
        >     
        >     
        >     > **Q**: Why is the example referencing PAPI.dll rather than Banking.Library.PAPI.dll  
        >     > **A**: The PAPI.dll file is outputted by the build which has it's assembly info defined as PAPI
  
###### 10.7.4 Update the meta elements with the relevant information.  
  
* Update the `<authors>` element with your teams email address.  
> For deployment packages, you will get an email at that address when your package is promoted. 
  
* Update `<description>` with your app/library description.  
* Update `<releaseNotes>` accordingly  
* Update `<tags>` with your NuGet package id with spaces instead of full stops  
> Eg: `Internal.Web.Minion.Core` to `Internal Web Minion Core`  
  
  
## Adding a new build/project to TeamCity  
  
- Go to the project where you want to add your new build  
> Eg: `https://teamcity/project.html?projectId=ConfigurationUtilities_BuildConfig&tab=projectOverview`  
  
- Click `Edit Project Settings` on the top right of the page. 
> if you do not see this, you do not have permissions, please log a call to give you project admin permissions [https://fw.minion/cfggenr](https://fw.minion/cfggenr)  
  
- Click `Create Build Configuration`  
- Select `Manual` on the drop down  
- Enter the `NuGet package id` as the `Name` of the new build configuration.  
- Select the `based on template` depending on the following:  
> GIT version control:  
> > Normal build: Build Template (Git)  
> > Config build: Config Build Template (Git)  
>  
> SVN version control: 
> > Normal build: Build Template (Svn)  
> > Config build: Config Build Template (Svn)  
  
- For SVN (normal and config based builds):  
> Fill in the `Repo-Path` parameter with your `branch/trunk reference`  
> > Eg: `trunk` or `branches/{MyBranchname}`  
  
- For Config builds:  
> Fill in the `OctopusProjectName` parameter with your Octopus project name  
> This will most likely be your config NuGet package id  
> > Eg: `Internal.Web.Minion.Api.Config`  
> > Fill this in even if you aren't going to use it yet  
  
- Click the `Create` button. 
> Notes:  
>  
> Don't worry about filling the other parameters if you don't need to, the defaults are fine  
  
- Click `Version Control Settings` on the left of the page  
  
- Click `Attach VCS root`  
  
- If you already created a VCS root for this project:  
> - Select the existing VCS url from the `Attach existing VCS root` drop down.  
> - Click `Attach`  
  
- If you have a new build and you haven't created one: 
> - Select GIT/SVN under the `Create new VCS root` drop down.  
> - Click `Create`  
>  
> #### For GIT  
> - Paste the repo url into the `Fetch URL` text box.  
> > Leave `Push URL` **blank**  
>  
> - Add the following to the `Branch specification` text box: `+:refs/heads/*`  
> - Change `Username style` to `Author Name and Email`  
> - Change `Authentication method` to `Uploaded Key` and select `build@mgsops.net`.  
> - Check the `Convert line-endings to CRLF` check box  
>  
> #### For SVN  
- Paste the repo url into the `URL` text box folled by `%Repo-Path%`.   
> > Eg: `https://dersvn02/repos/CasperRepo/Caiman/Applications/%Repo-Path%`  
>  
- Enter `MGSOPS\Build` in the `Username` field  
- Leave `Password` blank  
- Change `Externals support` to `Checkout, but ignore changes`  
> > Now `log a call` for someone in the DevOps team to enter the password - [https://fw.minion/cfgbuildp](https://fw.minion/cfgbuildp)  
  
> - Click `Create`  
  
> > Notes:  
>  
> > You do not have to wait for the password to be filled to continue with this process  
  
> Notes:  
>  
> If you setup a Config build template but your Octopus project doesn't exist yet, your build will fail but that is okay, it will succeed as soon as the project is promoted to LIVE  
  
- Click `Run` on the top right to run your build.  
  
### Any further questions/help -> [https://fw.minion/cfgbuildp](https://fw.minion/cfgbuildp)  