param(
	[string]$InitialMigration = "20191117141849_AddIsHandling",
	[string]$StartupProject = "..\Tolk.Web\Tolk.Web.csproj",
	[string]$Project = "..\Tolk.BusinessLogic\Tolk.BusinessLogic.csproj",
	[string]$OutputFolder = ".\bin",
	$dotnet = "dotnet"
	)

$n = 0
[bool]$IsStarted = $false
[string]$Previous = ""

Remove-Item $OutputFolder\*.sql

$prm = "ef", "migrations",  "script", "-o", ($OutputFolder + "\TolkMigrate.sql"), "--startup-project", $StartupProject, "-p", $Project , "-i", "--no-build"

& $dotnet $prm

$prm = "ef", "migrations",  "list", "--startup-project", $StartupProject, "-p", $Project, "--no-build"

$items = & $dotnet $prm | select

foreach ($item in $items) {
	If ($IsStarted -eq $true)
	{
		$prm = "ef", "migrations",  "script", $item ,  $Previous, "-o", ($OutputFolder + "\" + $Previous + ".sql"), "--startup-project", $StartupProject, "-p", $Project , "-i", "--no-build"
		& $dotnet $prm
	} ElseIf ($item -eq $InitialMigration)
	{
		$IsStarted = $true
	}
	$Previous = $item
}