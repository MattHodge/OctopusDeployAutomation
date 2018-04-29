Octopus Deploy Automation
===================

## Usage

* Modify `projects.yaml`
* Set environment variables

```bash
export OCTOPUS_URL=https://octopus-deploy.contoso.net
export OCTOPUS_API_KEY=API-XXXXXXXXXXXXX
```
* Run the script
```bash
./build.sh
```

## Development on MacOS

As Cake is being used to automate the creation of Octopus Deploy Projects, the following needs to be installed.

```bash
brew cask install mono-mdk
```


