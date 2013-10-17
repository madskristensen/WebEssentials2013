param($rootPath, $toolsPath, $package, $project)

$importLabel = "VsixCompress"
$targetsPropertyName = "VsixCompressTargets"

# When this package is installed we need to add a property
# to the current project, which points to the
# .targets file in the packages folder

function RemoveExistingKnownPropertyGroups($projectRootElement){
    # if there are any PropertyGroups with a label of "$importLabel" they will be removed here
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
#  <PropertyGroup Label="VsixCompress">
#    <VsixCompressTargets Condition=" '$(VsixCompressTargets)'=='' ">$([System.IO.Path]::GetFullPath( $(MSBuildProjectDirectory)\..\packages\VsixCompress.1.0.0.6\tools\vsix-compress.targets ))</VsixCompressTargets>
#  </PropertyGroup>

# Before modifying the project save everything so that nothing is lost
$DTE.ExecuteCommand("File.SaveAll")
CheckoutProjFileIfUnderScc
EnsureProjectFileIsWriteable

# Update the Project file to import the .targets file
$relPathToTargets = ComputeRelativePathToTargetsFile -startPath ($projItem = Get-Item $project.FullName) -targetPath (Get-Item ("{0}\tools\vsix-compress.targets" -f $rootPath))

$projectMSBuild = [Microsoft.Build.Construction.ProjectRootElement]::Open($projFile)

RemoveExistingKnownPropertyGroups -projectRootElement $projectMSBuild
$propertyGroup = $projectMSBuild.AddPropertyGroup()
$propertyGroup.Label = $importLabel

$importStmt = ('$([System.IO.Path]::GetFullPath( $(MSBuildProjectDirectory)\{0} ))' -f $relPathToTargets)
$propNuGetImportPath = $propertyGroup.AddProperty('VsixCompressTargets', "$importStmt");
$propNuGetImportPath.Condition = ' ''$(VsixCompressTargets)''=='''' ';

AddImportElementIfNotExists -projectRootElement $projectMSBuild

$projectMSBuild.Save()

"    VsixCompress has been installed into project [{0}]" -f $project.FullName| Write-Host -ForegroundColor DarkGreen
"    `nFor more info how to enable VsixCompress on build servers see http://sedodream.com/2013/06/06/HowToSimplifyShippingBuildUpdatesInANuGetPackage.aspx" | Write-Host -ForegroundColor DarkGreen