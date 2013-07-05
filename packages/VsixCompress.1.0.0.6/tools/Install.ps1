param($rootPath, $toolsPath, $package, $project)

$importLabel = "VsixCompress"
$targetsPropertyName = "VsixCompressTargets"

# When this package is installed we need to add a property
# to the current project, SlowCheetahTargets, which points to the
# .targets file in the packages folder

function RemoveExistingSlowCheetahPropertyGroups($projectRootElement){
    # if there are any PropertyGroups with a label of "SlowCheetah" they will be removed here
    $pgsToRemove = @()
    foreach($pg in $projectRootElement.PropertyGroups){
        if($pg.Label -and [string]::Compare($importLabel,$pg.Label,$true) -eq 0) {
            # remove this property group
            $pgsToRemove += $pg
        }
    }

    foreach($pg in $pgsToRemove){
        $pg.Parent.RemoveChild($pg)
    }
}

# TODO: Revisit this later, it was causing some exceptions
function CheckoutProjFileIfUnderScc(){
    # http://daltskin.blogspot.com/2012/05/nuget-powershell-and-tfs.html
    $sourceControl = Get-Interface $project.DTE.SourceControl ([EnvDTE80.SourceControl2])
    if($sourceControl.IsItemUnderSCC($project.FullName) -and $sourceControl.IsItemCheckedOut($project.FullName)){
        $sourceControl.CheckOutItem($project.FullName)
    }
}

function EnsureProjectFileIsWriteable(){
    $projItem = Get-ChildItem $project.FullName
    if($projItem.IsReadOnly) {
        "The project file is read-only. Please checkout the project file and re-install this package" | Write-Host -ForegroundColor Red
        throw;
    }
}

function ComputeRelativePathToTargetsFile(){
    param($startPath,$targetPath)   

    # we need to compute the relative path
    $startLocation = Get-Location

    Set-Location $startPath.Directory | Out-Null
    $relativePath = Resolve-Path -Relative $targetPath.FullName

    # reset the location
    Set-Location $startLocation | Out-Null

    return $relativePath
}

function GetSolutionDirFromProj{
    param($msbuildProject)

    if(!$msbuildProject){
        throw "msbuildProject is null"
    }

    $result = $null
    $solutionElement = $null
    foreach($pg in $msbuildProject.PropertyGroups){
        foreach($prop in $pg.Properties){
            if([string]::Compare("SolutionDir",$prop.Name,$true) -eq 0){
                $solutionElement = $prop
                break
            }
        }
    }

    if($solutionElement){
        $result = $solutionElement.Value
    }

    return $result
}

# we need to update the packageRestore.proj file to have the correct value for SolutionDir
function UpdatePackageRestoreSolutionDir (){
    param($pkgRestorePath, $solDirValue)
    if(!(Test-Path $pkgRestorePath)){
        throw ("pkgRestore file not found at {0}" -f $pkgRestorePath)
    }

    $solDirElement = $null
    $root = [Microsoft.Build.Construction.ProjectRootElement]::Open($pkgRestorePath)
    foreach($pg in $root.PropertyGroups){
        foreach($prop in $pg.Properties){
            if([string]::Compare("SlowCheetahSolutionDir",$prop.Label,$true) -eq 0){
                $solDirElement = $prop
                break
            }
        }
    }
    
    if($solDirElement){
        $solDirElement.Value = $solDirValue

        $root.Save()

    }
}

function AddImportElementIfNotExists(){
    param($projectRootElement)

    $foundImport = $false
    $importsToRemove = @()
    foreach($import in $projectRootElement.Imports){
        $importStr = $import.Project
        if(!$importStr){
            $importStr = ""
        }

        $currentLabel = $import.Label
        if(!$currentLabel){
            $currentLabel = ""
        }

        if([string]::Compare($importLabel,$currentLabel.Trim(),$true) -eq 0){
            # found the import no need to continue
            $foundImport = $true
            break
        }

        #if([string]::Compare("`$($targetsPropertyName)",$importStr.Trim(),$true) -eq 0){
        #    if(!$foundImport){
        #       # if it doesn't have a label then add one
        #        if([string]::IsNullOrWhiteSpace($import.Label)){
        #            $import.Label = $importLabel
        #        }
        #        $import.Condition="Exists('`$($targetsPropertyName)')"
        #
        #        $foundImport = $true
        #    }
        #    else{
        #        # if we already found an import, this must be a duplicate remove it
        #        $importsToRemove+=$import
        #    }
        #}
    }

    if(!$foundImport){
        # the import is not in the project, add it
        # <Import Project="$(VsixCompressImport)" Condition="Exists('$(VsizCompressTargets)')" Label="VsixCompress" />
        $importToAdd = $projectRootElement.AddImport("`$($targetsPropertyName)");
        $importToAdd.Condition = "Exists('`$($targetsPropertyName)')"
        $importToAdd.Label = $importLabel 
    }        
}


#########################
# Start of script here
#########################

$projFile = $project.FullName

# Make sure that the project file exists
if(!(Test-Path $projFile)){
    throw ("Project file not found at [{0}]" -f $projFile)
}

# use MSBuild to load the project and add the property

# This is what we want to add to the project
#  <PropertyGroup Label="SlowCheetah">
#      <SlowCheetah_EnableImportFromNuGet Condition=" '$(SC_EnableImportFromNuGet)'=='' ">true</SlowCheetah_EnableImportFromNuGet>
#      <SlowCheetah_NuGetImportPath Condition=" '$(SlowCheetah_NuGetImportPath)'=='' ">$([System.IO.Path]::GetFullPath( $(Filepath) ))</SlowCheetah_NuGetImportPath>
#      <SlowCheetahTargets Condition=" '$(SlowCheetah_EnableImportFromNuGet)'=='true' and Exists('$(SlowCheetah_NuGetImportPath)') ">$(SlowCheetah_NuGetImportPath)</SlowCheetahTargets>
#  </PropertyGroup>


# EnsureProjectFileIsWriteable
# Before modifying the project save everything so that nothing is lost
$DTE.ExecuteCommand("File.SaveAll")
CheckoutProjFileIfUnderScc
EnsureProjectFileIsWriteable

# Update the Project file to import the .targets file
$relPathToTargets = ComputeRelativePathToTargetsFile -startPath ($projItem = Get-Item $project.FullName) -targetPath (Get-Item ("{0}\tools\vsix-compress.targets" -f $rootPath))

$projectMSBuild = [Microsoft.Build.Construction.ProjectRootElement]::Open($projFile)

RemoveExistingSlowCheetahPropertyGroups -projectRootElement $projectMSBuild
$propertyGroup = $projectMSBuild.AddPropertyGroup()
$propertyGroup.Label = $importLabel

#$propEnableNuGetImport = $propertyGroup.AddProperty('SlowCheetah_EnableImportFromNuGet', 'true');
#$propEnableNuGetImport.Condition = ' ''$(SC_EnableImportFromNuGet)''=='''' ';

$importStmt = ('$([System.IO.Path]::GetFullPath( $(MSBuildProjectDirectory)\{0} ))' -f $relPathToTargets)
$propNuGetImportPath = $propertyGroup.AddProperty('VsixCompressTargets', "$importStmt");
$propNuGetImportPath.Condition = ' ''$(VsixCompressTargets)''=='''' ';

#$propImport = $propertyGroup.AddProperty('SlowCheetahTargets', '$(SlowCheetah_NuGetImportPath)');
#$propImport.Condition = ' ''$(SlowCheetah_EnableImportFromNuGet)''==''true'' and Exists(''$(SlowCheetah_NuGetImportPath)'') ';

AddImportElementIfNotExists -projectRootElement $projectMSBuild

$projectMSBuild.Save()

# now update the packageRestore.proj file with the correct path for SolutionDir
# $solnDirFromProj = GetSolutionDirFromProj -msbuildProject $projectMSBuild
# if($solnDirFromProj) {
#    $pkgRestorePath = (Join-Path (get-item $project.FullName).Directory 'packageRestore.proj')
#    UpdatePackageRestoreSolutionDir -pkgRestorePath $pkgRestorePath -solDirValue $solnDirFromProj
#}
#else{
#    $msg = @"
#    SolutionDir property not found in project [{0}].
#    Have you enabled NuGet Package Restore? This is required for build server support.
#    You may need to enable it and to enable it and re-install this package
#"@ 
#    $msg -f $project.Name | Write-Host -ForegroundColor Red
#}

"    VsixCompress has been installed into project [{0}]" -f $project.FullName| Write-Host -ForegroundColor DarkGreen
"    `nFor more info how to enable VsixCompress on build servers see http://sedodream.com/2012/12/24/SlowCheetahBuildServerSupportUpdated.aspx" | Write-Host -ForegroundColor DarkGreen