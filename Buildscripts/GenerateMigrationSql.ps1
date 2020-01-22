param(
	[string]$InitialMigration = "20191117141849_AddIsHandling",
	[string]$StartupProject = "..\Tolk.Web\Tolk.Web.csproj",
	[string]$Project = "..\Tolk.BusinessLogic\Tolk.BusinessLogic.csproj",
	[string]$OutputFolder = ".\bin"
	)

[bool]$IsStarted = $false
[string]$Previous = ""

Remove-Item $OutputFolder\*.sql

dotnet ef migrations script -o $OutputFolder\TolkMigrate.sql --startup-project $StartupProject -p $Project -i --no-build

dotnet ef migrations list --startup-project $StartupProject -p $Project --no-build | select | 
foreach {
	If ($IsStarted -eq $true)
	{
		dotnet ef migrations script $_  $Previous -o $OutputFolder\$Previous.sql --startup-project $StartupProject -p $Project -i --no-build
	} ElseIf ($_ -eq $InitialMigration)
	{
		$IsStarted = $true
	}
	$Previous = $_
}