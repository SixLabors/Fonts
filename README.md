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

### Backers

Support us with a monthly donation and help us continue our activities. [[Become a backer](https://opencollective.com/imagesharp#backer)]

<a href="https://opencollective.com/imagesharp/backer/0/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/0/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/1/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/1/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/2/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/2/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/3/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/3/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/4/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/4/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/5/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/5/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/6/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/6/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/7/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/7/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/8/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/8/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/9/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/9/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/10/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/10/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/11/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/11/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/12/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/12/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/13/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/13/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/14/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/14/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/15/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/15/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/16/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/16/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/17/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/17/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/18/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/18/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/19/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/19/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/20/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/20/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/21/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/21/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/22/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/22/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/23/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/23/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/24/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/24/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/25/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/25/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/26/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/26/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/27/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/27/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/28/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/28/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/backer/29/website" target="_blank"><img src="https://opencollective.com/imagesharp/backer/29/avatar.svg"></a>

### Sponsors

Become a sponsor and get your logo on our README on Github with a link to your site. [[Become a sponsor](https://opencollective.com/imagesharp#sponsor)]

<a href="https://opencollective.com/imagesharp/sponsor/0/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/0/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/1/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/1/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/2/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/2/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/3/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/3/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/4/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/4/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/5/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/5/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/6/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/6/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/7/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/7/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/8/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/8/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/9/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/9/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/10/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/10/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/11/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/11/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/12/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/12/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/13/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/13/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/14/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/14/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/15/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/15/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/16/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/16/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/17/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/17/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/18/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/18/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/19/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/19/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/20/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/20/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/21/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/21/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/22/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/22/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/23/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/23/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/24/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/24/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/25/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/25/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/26/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/26/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/27/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/27/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/28/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/28/avatar.svg"></a>
<a href="https://opencollective.com/imagesharp/sponsor/29/website" target="_blank"><img src="https://opencollective.com/imagesharp/sponsor/29/avatar.svg"></a>
