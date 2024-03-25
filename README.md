# DEH-EASYSML

The DEH-EASysML adapter is a .Net plugin Application for [Sparx Enterprise Architect](https://sparxsystems.com/products/ea/index.html) that make use of the [Digital Engineering Hub Pathfinder Common library](https://github.com/RHEAGROUP/DEHP-Common) which is available as Nuget package.
It allows users to interactivly exchange data between models built with the Enterprise Architect software and an ECSS-E-TM-10-25A data source.

The DEH-EASysML is compatible with version [15.2](https://sparxsystems.com/products/ea/15.2/index.html) and any newer version of the Enterprise Architect software.

## Installing the DEH-EASysML adapter

- Download the [latest release](https://github.com/RHEAGROUP/DEH-EASYSML/releases/latest).
  - If you are running EA 15.2, please uses the 32bits version of the installer
  - If you are running any newer version, please uses the installer related to your installed architecture
- Close any running instance of Enterprise Architect.
- Run the downloaded installer.
- Specify the location to install the Add-In.
- Run Enterprise Architect.

## Operating the DEH-EASysML adapter

- After installing the adpter.
- A Comet icon ![Comet](https://github.com/RHEAGROUP/DEH-CommonJ/blob/master/src/main/resources/icon16.png?raw=true) in the main toolbar on the *Publish* Category gives access to show/hide all the views of the adapter.
- The Hub panel is the one that allows to connect to a Comet webservice/ECSS-E-TM-10-25A data source. Once there is a Comet model open, and a SysML project open. Mapping between models can achieved in any direction.
- To initialize a new mapping, there is a Map action available in the context menus of Project browsers such as the one from Enterprise Architect and the ElementDefinitions and Requirements ones from the adapter panels.
  - In addition to the previous action, in the entry *Specialize > Comet > Map* the action will map all mappable objects contained in the selected Package (or the Parent Package of an element).
- The Impact View panel is where Impact on target models can be previewed/transfered. Also from this view mapping information can be loaded/saved.
- The *Notification Window* displays the output of the adapter. This view can be found here: *Start > Explore > Browse > System Output*

## License

The libraries contained in the DEH-EASysML adapter are provided to the community under the GNU Lesser General Public License. Because we make the software available with the LGPL, it can be used in both open source and proprietary software without being required to release the source code of your own components.

## Contributions

Contributions to the code-base are welcome. However, before we can accept your contributions we ask any contributor to sign the Contributor License Agreement (CLA) and send this digitaly signed to s.gerene@rheagroup.com. You can find the CLA's in the CLA folder.
