Octopus Deploy Automation
===================

If you often create similar Octopus Deploy projects, this Cake script will help you by defining Octopus Projects as templates, and cloning them into new projects.

Once cloned, the desired parameters and settings for the project can be updated.

All configuration is driven by a YAML file.

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


