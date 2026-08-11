storybrew is an osu! storyboard editor for people that don't want to form an intimate relationship with the Ctrl-L shortcut: It lets you see changes to code or sprite textures as soon as you save them.

[Download the latest version](https://github.com/Damnae/storybrew/releases/latest), or learn more on the [Wiki](https://github.com/Damnae/storybrew/wiki/Getting-Started-%28Without-Programming%29).

[![](http://puu.sh/po6Tt/00d807e1ae.png)](https://github.com/Damnae/storybrew/wiki)

## Building

Initialize the BrewLib submodule and build the x64 editor:

```powershell
git submodule update --init --recursive
dotnet build storybrew.sln -c Debug
```

NuGet restores the x64 BASS and BASS_FX native libraries from
`ppy.osu.Framework.NativeLibs`; `ManagedBass` remains the architecture-neutral
managed wrapper. The editor requires an x64 .NET 8 SDK because it compiles
storyboard scripts at runtime.
