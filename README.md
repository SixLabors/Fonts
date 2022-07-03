<h1 align="center">

<img src="https://raw.githubusercontent.com/SixLabors/Branding/main/icons/fonts/sixlabors.fonts.512.png" alt="SixLabors.Fonts" width="256"/>
<br/>
SixLabors.Fonts
</h1>

<div align="center">

[![Build Status](https://img.shields.io/github/workflow/status/SixLabors/Fonts/Build/main)](https://github.com/SixLabors/Fonts/actions)
[![codecov](https://codecov.io/gh/SixLabors/Fonts/branch/main/graph/badge.svg)](https://codecov.io/gh/SixLabors/Fonts)
[![License: Apache 2.0](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

[![GitHub issues](https://img.shields.io/github/issues/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/issues)
[![GitHub stars](https://img.shields.io/github/stars/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/network)

</div>

**SixLabors.Fonts** is a new cross-platform font loading and drawing library.

## License

- Fonts is licensed under the [Apache License, Version 2.0](https://opensource.org/licenses/Apache-2.0)
- An alternative Commercial Support License can be purchased **for projects and applications requiring support**.
Please visit https://sixlabors.com/pricing for details.

## Support Six Labors

Support the efforts of the development of the Six Labors projects. 
 - [Purchase a Commercial Support License :heart:](https://sixlabors.com/pricing/)
 - [Become a sponsor via GitHub Sponsors :heart:]( https://github.com/sponsors/SixLabors)
 - [Become a sponsor via Open Collective :heart:](https://opencollective.com/sixlabors)

## Documentation

- [Detailed documentation](https://sixlabors.github.io/docs/) for the Fonts API is available. This includes additional conceptual documentation to help you get started.
- Our [Samples Repository](https://github.com/SixLabors/Samples/tree/main/ImageSharp) is also available containing buildable code samples demonstrating common activities.

## Questions

- Do you have questions? We are happy to help! Please [join our Discussions Forum](https://github.com/SixLabors/Fonts/discussions/category_choices).
- Please read our [Contribution Guide](https://github.com/SixLabors/Fonts/blob/main/.github/CONTRIBUTING.md) before opening issues or pull requests!

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
- Reading font description (name, family, subname etc plus other string metadata).
- Loading OpenType fonts with with CFF1 and True Type outlines.
- Loading True Type fonts.
- Loading [WOFF fonts](https://www.w3.org/Submission/WOFF/).
- Loading [WOFF2 fonts](https://www.w3.org/TR/WOFF2).
- Load all compatible fonts from local machine store.
- Suppord for line breaking based on [UAX 14](https://www.unicode.org/reports/tr14/)
- Support for rendering left to right, right to left and bidirectional text.
- Support for ligatures.
- Support for advanced OpenType features glyph substitution ([GSUB](https://docs.microsoft.com/en-us/typography/opentype/spec/gsub)) and glyph positioning ([GPOS](https://docs.microsoft.com/en-us/typography/opentype/spec/gpos))

## API Examples

### Read font description

```c#
FontDescription description = null;
using(var fs = File.OpenRead("Font.ttf")){
    description = FontDescription.Load(fs); // once it has loaded the data the stream is no longer required and can be disposed of
}

string name = description.FontName(CultureInfo.InvariantCulture);

```

### Populating a font collection

```c#
FontCollection fonts = new FontCollection();
FontFamily font1 = fonts.Add("./path/to/font1.ttf");
FontFamily font2 = fonts.Add("./path/to/font2.woff");

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
