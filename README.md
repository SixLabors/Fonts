<h1 align="center">

<img src="https://raw.githubusercontent.com/SixLabors/Branding/master/icons/fonts/sixlabors.fonts.512.png" alt="SixLabors.Fonts" width="256"/>
<br/>
SixLabors.Fonts
</h1>

<div align="center">

[![Build status](https://ci.appveyor.com/api/projects/status/26dab7lf874b5ib3/branch/master?svg=true)](https://ci.appveyor.com/project/six-labors/fonts/branch/master)
[![codecov](https://codecov.io/gh/SixLabors/Fonts/branch/master/graph/badge.svg)](https://codecov.io/gh/SixLabors/Fonts)
[![GitHub license](https://img.shields.io/badge/license-Apache%202-blue.svg)](https://raw.githubusercontent.com/SixLabors/Fonts/master/LICENSE)

[![Join the chat at https://gitter.im/SixLabors/Shapes](https://badges.gitter.im/SixLabors/Fonts.svg)](https://gitter.im/SixLabors/Fonts?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![GitHub issues](https://img.shields.io/github/issues/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/issues)
[![GitHub stars](https://img.shields.io/github/stars/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/SixLabors/Fonts.svg)](https://github.com/SixLabors/Fonts/network)

</div>

**SixLabors.Fonts** is a new cross-platform font loadings and drawing library.

### Installation

**Pre-release downloads**

At present the code is pre-release we have initial pre-releases availible on [nuget](https://www.nuget.org/packages/SixLabors.Fonts/).

We also have a [MyGet package repository](https://www.myget.org/gallery/SixLabors) - for bleeding-edge / development NuGet releases.

### Manual build

If you prefer, you can compile SixLabors.Shapes yourself (please do and help!), you'll need:

- [Visual Studio 2017](https://www.visualstudio.com/en-us/news/releasenotes/vs2017-relnotes)
- The [.NET Core 1.0 SDK Installer](https://www.microsoft.com/net/core#windows) - Non VSCode link.

To clone it locally click the "Clone in Windows" button above or run the following git commands.

```bash
git clone https://github.com/SixLabors/Fonts.git
```

### Features
- Reading font description (name, family, subname etc plus other string metadata)
- Loading True type fonts
- Loading WOFF fonts
- Load all compatable fornts from local machine store (windows only at the moment)

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

### Popluating a font collection

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

Lead
- [Scott Williams](https://github.com/tocsoft)

Core Team
- [Dirk Lemstra](https://github.com/dlemstra)
- [Anton Firsov](https://github.com/antonfirsov)
- [James Jackson-South](https://github.com/jimbobsquarepants)
