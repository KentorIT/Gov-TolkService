Connect-AzureRmAccount -SubscriptionId "b5d73a6f-1a31-4c49-b52d-d7213d39db71"

$resource = Invoke-AzureRmResourceAction -ResourceGroupName Dev -ResourceType Microsoft.Web/sites/config -ResourceName "tolkdev/appsettings" -Action list -ApiVersion 2016-08-01 -Force
$resource.properties | ConvertTo-Json -depth 100 | Out-File .\bin\web\dev.json -Encoding utf8

$resource = Invoke-AzureRmResourceAction -ResourceGroupName Dev -ResourceType Microsoft.Web/sites/config -ResourceName "tolkdevapi/appsettings" -Action list -ApiVersion 2016-08-01 -Force
$resource.properties | ConvertTo-Json -depth 100 | Out-File .\bin\api\dev.json -Encoding utf8

$resource = Invoke-AzureRmResourceAction -ResourceGroupName systest -ResourceType Microsoft.Web/sites/config -ResourceName "tolksystest/appsettings" -Action list -ApiVersion 2016-08-01 -Force
$resource.properties | ConvertTo-Json -depth 100 | Out-File .\bin\web\systest.json -Encoding utf8

$resource = Invoke-AzureRmResourceAction -ResourceGroupName SysTest -ResourceType Microsoft.Web/sites/config -ResourceName "tolksystestapi/appsettings" -Action list -ApiVersion 2016-08-01 -Force
$resource.properties | ConvertTo-Json -depth 100 | Out-File .\bin\api\systest.json -Encoding utf8

Set-AzureRmContext -SubscriptionId "a4f37999-6226-48f9-8c5a-5e72d76572bb"

$resource = Invoke-AzureRmResourceAction -ResourceGroupName ProdWebResource -ResourceType Microsoft.Web/sites/config -ResourceName "KamkTolkWebProd/appsettings" -Action list -ApiVersion 2016-08-01 -Force
$resource.properties | ConvertTo-Json -depth 100 | Out-File .\bin\web\prod.json -Encoding utf8

$resource = Invoke-AzureRmResourceAction -ResourceGroupName ProdWebResource -ResourceType Microsoft.Web/sites/config -ResourceName "KamkTolkApiProd/appsettings" -Action list -ApiVersion 2016-08-01 -Force
$resource.properties | ConvertTo-Json -depth 100 | Out-File .\bin\api\prod.json -Encoding utf8
