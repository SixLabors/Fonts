---
layout: default
title: Getting started
menu-order : 1
---

## Getting started

First make sure you have `SixLabors.Fonts` installed. [Check out out installation guide]({{ site.baseurl }}{% link docs/getting-started.md %}).

Now the library is installed in your project you can start working with it.

### Loading Fonts

To work with fonts you first have to install them into a `FontCollection`. The `FontCollection` is the root object that manages font styles and families. See [Font Collections]({{ site.baseurl }}{% link docs/font-collections.md %})) for more details.

>We have a pre-configured `FontCollection` at `SystemFonts` which is a read only collection with access to all the supported fonts installed in your operating system that the process can find.

You can install a font in one of 2 ways eather from a stream or a file. Installing a new font will return a `FontFamily` object representing the newly installed font.

#### File Path
```c#
var collection = new FontCollection();
var font = collection.Install(@"c:\path\to\font.ttf");
```

#### Stream
```c#
var collection = new FontCollection();
var font = collection.Install(stream);
```
Assumes you have a seekable stream set at the start of the font, this could be a memory stream, a file stream or any other type.

### Using Fonts

`Font` objects are immutable to change any of the settings of a font you need to create a new font passing in the old one (or a `FontFamily`).

You need to make a `Font` instance to start working with glyphs.

To make a font you will need to find the `FontFamily` you are interested in, and instanciate a new `Font` object with your required size and style.

```c#
var family = collection.Find("Arial"); //assumes arial has been installed in to the collection.
var font = new Font(family, 50, FontStyle.Bold); //assumes the version of arial that was installed was a bold veriant.
```