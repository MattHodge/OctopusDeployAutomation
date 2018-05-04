$ErrorActionPreference = 'Stop'

# Copy Files from Image

Write-Output "Grabbing required files from Octopus Server"

$octopusContainerId = & docker ps -aq

$filesFromOctopusServer = @(
    "C:\Program Files\Octopus Deploy\Octopus\Octopus.Client.dll",
    "C:\Program Files\Octopus Deploy\Octopus\Newtonsoft.Json.dll"
)

foreach ($item in $filesFromOctopusServer) {
    if (!(Test-Path -Path $item))
    {
        Start-Process docker -ArgumentList "cp $($octopusContainerId):`"$($item)`" $($pwd)" -Wait -NoNewWindow
    }
}

$OctopusURI = "http://$($env:LOCAL_MACHINE_IP):81" #Octopus URL

$APIKeyPurpose = "PowerShell" #Brief text to describe the purpose of your API Key.

#Adding libraries. Make sure to modify these paths acording to your environment setup.
Add-Type -Path "Newtonsoft.Json.dll"
Add-Type -Path "Octopus.Client.dll"

Write-Output "Attempting to connect to Octopus Server"

#Creating a connection
$endpoint = new-object Octopus.Client.OctopusServerEndpoint $OctopusURI
$repository = new-object Octopus.Client.OctopusRepository $endpoint

#Creating login object
$LoginObj = New-Object Octopus.Client.Model.LoginCommand
$LoginObj.Username = $env:TEST_OCTOPUS_USERNAME
$LoginObj.Password = $env:TEST_OCTOPUS_PASSWORD

#Loging in to Octopus
$repository.Users.SignIn($LoginObj)

#Getting current user logged in
$UserObj = $repository.Users.GetCurrent()

#Creating API Key for user. This automatically gets saved to the database.
$ApiObj = $repository.Users.CreateApiKey($UserObj, $APIKeyPurpose)

Write-Output "Octopus API Key: $($ApiObj.ApiKey)"

#############################
# CREATE ENVIRONMENT
#############################

Start-Process octo -ArgumentList "create-environment --name Testing --server $($OctopusURI) --apikey $($ApiObj.ApiKey)" -Wait -NoNewWindow
