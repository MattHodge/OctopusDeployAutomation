# Copy Files from Image

$octopusContainerId = & docker ps -aq

Start-Process docker -ArgumentList "cp $($octopusContainerId):`"C:\Program Files\Octopus Deploy\Octopus\Octopus.Client.dll`" ." -Wait -NoNewWindow
Start-Process docker -ArgumentList "cp $($octopusContainerId):`"C:\Program Files\Octopus Deploy\Octopus\Newtonsoft.Json.dll`" ." -Wait -NoNewWindow

$OctopusURI = "http://localhost:81" #Octopus URL

$APIKeyPurpose = "PowerShell" #Brief text to describe the purpose of your API Key.

#Adding libraries. Make sure to modify these paths acording to your environment setup.
Add-Type -Path "Newtonsoft.Json.dll"
Add-Type -Path "Octopus.Client.dll"

#Creating a connection
$endpoint = new-object Octopus.Client.OctopusServerEndpoint $OctopusURI
$repository = new-object Octopus.Client.OctopusRepository $endpoint

#Creating login object
$LoginObj = New-Object Octopus.Client.Model.LoginCommand
$LoginObj.Username = $OctopusUsername
$LoginObj.Password = $OctopusPassword

#Loging in to Octopus
$repository.Users.SignIn($LoginObj)

#Getting current user logged in
$UserObj = $repository.Users.GetCurrent()

#Creating API Key for user. This automatically gets saved to the database.
$ApiObj = $repository.Users.CreateApiKey($UserObj, $APIKeyPurpose)

#############################
# CREATE ENVIRONMENT
#############################

& octo create-environment --name Testing --server http://localhost --apikey $ApiObj.ApiKey
