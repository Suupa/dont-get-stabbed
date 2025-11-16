<div class="header" align="center">
<img alt="Don't Get Stabbed" width="880" height="300" src="https://raw.githubusercontent.com/Suupa/asset-dump/4c087b99ecb4838a2ed0c457e823d037ad11659b/github-logo.svg">
</div>

*Don't Get Stabbed* is a mechanically deep Multiplayer Role Playing Game that simulates a giant prison complex filled with real people.  To give a general idea of what the game has to offer, here are some random examples of roles the player can take on*:
- a hardened criminal shot caller who vies for power against rival gangs ⬜ <!--✅-->
- a no-nonsense Correctional Officer who tries to keep his inmates in line ✅
- a Confidential Informant who tries to balance his standing with both the gangs and the COs to fuel his drug addiction ⬜
- a Chaplain trying to save some souls among the damned ⬜
- a psychologist trying to decrease tensions in genpop with inmate group sessions ⬜
- an overworked Warden having to oversee the execution of a deadrow inmate while understaffed ⬜
- a corrupt CO smuggling in contraband for one of the gangs ⬜
- ..

And here are some random flavor examples of things you are able to do*:

- keister (hide in your ass) a shiv (prison made knife) ✅
- discipline a new inmate for accidentally stepping into another car's (faction) territory ⬜
- charm a CO until he gives you a job in the mail room ⬜
- pass kites (written messages) around from cell to cell, but be careful who might intercept them! ⬜
- search the inmates for contraband when they leave the workshop ⬜
- ..

*(\* The project is still in early development. Unchecked features are not yet in the game.)*


This project is a fork of Space Station 14 and runs on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox).


<!--
## Links

<div class="header" align="center">

[Website](TODO) | [Discord](https://discord.gg/Rk9wnKswpz) | [Patreon](TODO) | [Steam](TODO) | [Standalone Download](TODO)

</div>
-->

<!--
## Documentation/Wiki

(will be added later)

Our [docs site](https://docs.spacestation14.com/) has documentation on SS14's content, engine, game design, and more.
Additionally, see these resources for license and attribution information:
- [Robust Generic Attribution](https://docs.spacestation14.com/en/specifications/robust-generic-attribution.html)
- [Robust Station Image](https://docs.spacestation14.com/en/specifications/robust-station-image.html)

We also have lots of resources for new contributors to the project.
-->

## Contributing

We are happy to accept contributions from anybody. Get in [Discord](https://discord.gg/Rk9wnKswpz) if you want to help. We've got a [list of issues](https://github.com/Suupa/dont-get-stabbed/issues) that need to be done and anybody can pick them up. Don't be afraid to ask for help either!
Just make sure your changes and pull requests are in accordance with the [SS14 contribution guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html)

## Building

1. Clone this repo:
```shell
git clone https://github.com/Suupa/dont-get-stabbed.git
```
2. Go to the project folder and run `RUN_THIS.py` to initialize the submodules and load the engine:
```shell
cd dont-get-stabbed
python RUN_THIS.py
```
3. Compile the solution:
```shell
dotnet build
```

4. Run the game:

Run the server using `dotnet run --project Content.Server`.

Run the client using `dotnet run --project Content.Client`.


<!--[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)-->

## License

All code for the content repository is licensed under the [MIT license](https://github.com/Suupa/dont-get-stabbed/blob/master/LICENSE.TXT).

Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and copyright specified in the metadata file. For example, see the [metadata for a crowbar](https://github.com/Suupa/dont-get-stabbed/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

> [!NOTE]
> Some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.
