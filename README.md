<br />

<p align="center">
    <img src="https://raw.githubusercontent.com/akhansari/extLauncher/main/logo.png" alt="extLauncher logo" height="140">
</p>

<p align="center">
<!--
    <a href="https://github.com/akhansari/extLauncher/actions" title="actions"><img src="https://github.com/akhansari/akhansari/actions/workflows/build.yml/badge.svg?branch=main" alt="actions build" /></a>-->
<!--
    <a href="https://www.nuget.org/packages/extLauncher/" title="nuget"><img src="https://img.shields.io/nuget/vpre/extLauncher" alt="version" /></a>
    <a href="https://www.nuget.org/stats/packages/extLauncher?groupby=Version" title="stats"><img src="https://img.shields.io/nuget/dt/extLauncher" alt="download" /></a> -->
    <a href="./LICENSE.md" title="license"><img src="https://img.shields.io/github/license/akhansari/extLauncher" alt="license" /></a>
</p>

<br />

ExtLauncher is a dotnet tool to search and launch quickly projects in the user's preferred application. ExtLauncher is maintained by folks at [D-EDGE](https://www.d-edge.com/).

# Getting Started

Install extLauncher as a global dotnet tool

``` bash
dotnet tool install extLauncher -g
``` 

or as a dotnet local tool

``` bash
dotnet new tool-manifest
dotnet tool install extLauncher
```` 

# Usage

```
USAGE:
    extLauncher [OPTIONS]

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    search             (Default) Type to search. Arrows Up/Down to navigate. Enter to launch the file
    index <pattern>    Indexes all files recursively with a specific pattern which can be a wildcard (default) or a regular expression (--regex)
    deindex            Clears the current index
    info               Prints the current pattern and all the indexed files
    refresh            Updates the current index
```

# License

[MIT](./LICENSE.md)
