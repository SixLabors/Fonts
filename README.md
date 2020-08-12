<h1 align="center">

<img src="https://raw.githubusercontent.com/SixLabors/Branding/master/icons/fonts/sixlabors.fonts.512.png" alt="SixLabors.Fonts" width="256"/>
<br/>
SixLabors.Fonts
</h1>

<div align="center">

[![Build Status](https://img.shields.io/github/workflow/status/SixLabors/Fonts/Build/master)](https://github.com/SixLabors/Fonts/actions)
[![codecov](https://codecov.io/gh/SixLabors/Fonts/branch/master/graph/badge.svg)](https://codecov.io/gh/SixLabors/Fonts)
[![License: Apache 2.0](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

[![GitHub issues](https://img.shields.io/github/issues/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/issues)
[![GitHub stars](https://img.shields.io/github/stars/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/network)

</div>

**SixLabors.Fonts** is a new cross-platform font loading and drawing library.

## License
  
- Fonts is licensed under the [Apache License, Version 2.0](https://opensource.org/licenses/Apache-2.0)  
- An alternative Commercial License can be purchased for projects and applications requiring support.
Please visit https://sixlabors.com/pricing for details.

## Documentation

- [Detailed documentation](https://sixlabors.github.io/docs/) for the Fonts API is available. This includes additional conceptual documentation to help you get started.
- Our [Samples Repository](https://github.com/SixLabors/Samples/tree/master/ImageSharp) is also available containing buildable code samples demonstrating common activities.

## Questions

- Do you have questions? We are happy to help! Please [join our Discussions Forum](https://github.com/SixLabors/Fonts/discussions/category_choices).
- Please read our [Contribution Guide](https://github.com/SixLabors/Fonts/blob/master/.github/CONTRIBUTING.md) before opening issues or pull requests!

## Code of Conduct  
This project has adopted the code of conduct defined by the [Contributor Covenant](https://contributor-covenant.org/) to clarify expected behavior in our community.
For more information, see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## Installation

Install stable releases via Nuget; development releases are available via MyGet.

| Package Name                   | Release (NuGet) | Nightly (MyGet) |
|--------------------------------|-----------------|-----------------|
| `SixLabors.Fonts`         | [![NuGet](https://img.shields.io/nuget/v/SixLabors.Fonts.svg)](https://www.nuget.org/packages/SixLabors.Fonts/) | [![MyGet](https://img.shields.io/myget/sixlabors/v/SixLabors.Fonts.svg)](https://www.myget.org/feed/sixlabors/package/nuget/SixLabors.Fonts) |

## Manual build

If you prefer, you can compile Fonts yourself (please do and help!)

- Using [Visual Studio 2019](https://visualstudio.microsoft.com/vs/)
  - Make sure you have the latest version installed
  - Make sure you have [the .NET Core 3.1 SDK](https://www.microsoft.com/net/core#windows) installed

Alternatively, you can work from command line and/or with a lightweight editor on **both Linux/Unix and Windows**:

- [Visual Studio Code](https://code.visualstudio.com/) with [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- [.NET Core](https://www.microsoft.com/net/core#linuxubuntu)

To clone Fonts locally, click the "Clone in [YOUR_OS]" button above or run the following git commands:

```bash
git clone https://github.com/SixLabors/Fonts
```

If working with Windows please ensure that you have enabled log file paths in git (run as Administrator).

```bash
git config --system core.longpaths true
```

### Submodules

This repository contains [git submodules](https://blog.github.com/2016-02-01-working-with-submodules/). To add the submodules to the project, navigate to the repository root and type:

``` bash
git submodule update --init --recursive
```

### Features
- Reading font description (name, family, subname etc plus other string metadata)
- Loading True type fonts
- Loading WOFF fonts
- Load all compatible fonts from local machine store (windows only at the moment)

#### Limitations
we currently only support otf and woff fonts with True Type outlines.

## API Examples

### Read font description

```c#
FontDescription description = null;
using(var fs = File.OpenReader("Font.ttf")){
    description = FontDescription.Load(fs); // once it has loaded the data the stream is no longer required and can be disposed of
}

string name = description.FontName;

```

### Populating a font collection

```c#
FontCollection fonts = new FontCollection();
FontFamily font1 = fonts.Install("./path/to/font1.ttf");
FontFamily font2 = fonts.Install("./path/to/font2.woff");

```

### How can you help?

Please... Spread the word, contribute algorithms, submit performance improvements, unit tests.

### Projects using SixLabors.Fonts

* [SixLabors.ImageSharp](https://github.com/jimBobSquarePants/ImageSharp) - cross platform, fully managed, image manipulation and drawing library.

### The SixLabors.Fonts Team

- [Scott Williams](https://github.com/tocsoft)
- [Dirk Lemstra](https://github.com/dlemstra)
- [Anton Firsov](https://github.com/antonfirsov)
- [James Jackson-South](https://github.com/jimbobsquarepants)
- [Brian Popow](https://github.com/brianpopow)

## Sponsor Six Labors

Support the efforts of the development of the Six Labors projects. [[Become a sponsor :heart:](https://opencollective.com/sixlabors#sponsor)]

### Platinum Sponsors
Become a platinum sponsor with a monthly donation of $2000 (providing 32 hours of maintenance and development) and get 2 hours of dedicated support (remote support available through chat or screen-sharing) per month.

In addition you get your logo (large) on our README on GitHub and the home page (large) of sixlabors.com

<a href="https://opencollective.com/sixlabors/tiers/platinum-sponsors/0/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/platinum-sponsors/0/avatar.svg?avatarHeight=192"></a>

### Gold Sponsors
Become a gold sponsor with a monthly donation of $1000 (providing 16 hours of maintenance and development) and get 1 hour of dedicated support (remote support available through chat or screen-sharing) per month.

In addition you get your logo (large) on our README on GitHub and the home page (medium) of sixlabors.com

<a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/0/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/0/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/1/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/1/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/2/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/2/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/3/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/3/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/4/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/4/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/5/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/5/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/6/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/6/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/7/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/7/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/8/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/8/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/9/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/9/avatar.svg?avatarHeight=156"></a><a href="https://opencollective.com/sixlabors/tiers/gold-sponsors/10/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/gold-sponsors/10/avatar.svg?avatarHeight=156"></a>

### Silver Sponsors
Become a silver sponsor with a monthly donation of $500 (providing 8 hours of maintenance and development) and get your logo (medium) on our README on GitHub and the product pages of sixlabors.com

<a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/0/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/0/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/1/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/1/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/2/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/2/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/3/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/3/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/4/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/4/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/5/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/5/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/6/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/6/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/7/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/7/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/8/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/8/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/9/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/9/avatar.svg?avatarHeight=128"></a><a href="https://opencollective.com/sixlabors/tiers/silver-sponsors/10/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/silver-sponsors/10/avatar.svg?avatarHeight=128"></a>

### Bronze Sponsors
Become a bronze sponsor with a monthly donation of $100 and get your logo (small) on our README on GitHub.

<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/0/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/0/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/1/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/1/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/2/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/2/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/3/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/3/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/4/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/4/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/5/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/5/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/6/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/6/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/7/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/7/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/8/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/8/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/9/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/9/avatar.svg?avatarHeight=96"></a>
<a href="https://opencollective.com/sixlabors/tiers/bronze-sponsors/10/website" target="_blank"><img src="https://opencollective.com/sixlabors/tiers/bronze-sponsors/10/avatar.svg?avatarHeight=96"></a>