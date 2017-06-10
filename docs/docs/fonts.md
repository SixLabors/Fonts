---
layout: default
title: Font Instance
---

## Working with a Font

`Font`s are immutable all properties on a font object must be changed by instanciating a new `Font` class with the correct constructor overload

```c#
var newFont = new Font(oldFont, 50, FontStyle.Bold);
``` 

###  `Font` constructors

#### `Font(FontFamily family, float size, FontStyle style)`

Creates a new instance based on a `FontFamily` sourced from a `FontCollection` a new size in points, and a font style.

#### `Font(FontFamily family, float size)`

Creates a new instance based on a `FontFamily` sourced from a `FontCollection` and a new size in points

#### `Font(Font prototype, FontStyle style)`

Creates a new instance based on a previous `Font` prototype to clone properties from and the overridden style.

#### `Font(Font prototype, float size, FontStyle style)`

Creates a new instance based on a previous `Font` prototype to clone properties from, the overridden size in points, and the overridden style.

#### `Font(Font prototype, float size)`

Creates a new instance based on a previous `Font` prototype to clone properties from, and the overridden size in points.

###  `Font` properties

#### `Family`

The `FontFamily` that this `Font` belongs is an instance of.

#### `Name`

The full name of the font.

#### `Size`

The size in point of this font.

#### `Italic`

Will the unerlying font render in italic. 

> This might be false even if a `FontStyle.Italic` is requested. This will happen if a bold font hasn't been installed into root collection.

#### `Bold`

Will the unerlying font render in bold. 

> This might be false even if a `FontStyle.Bold` is requested. This will happen if a bold font hasn't been installed into root collection.


#### `EmSize`

The EM size of this font in font units.

###  `Font` methods

#### `GetGlyph(char character)`

Returns the `Glyph` that shoul dbe renderd for the character.
