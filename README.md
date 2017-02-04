# Fonts
Font loading and drawing library.

## Features
- Reading font description (name, family, subname etc plus other string metadata)

## API Examples

### Read font description

```c#
FontDescription description = null;
using(var fs = File.OpenReader("Font.ttf")){
    description = FontDescription.Load(fs); // once it has loaded the data the stream is no longer required and can be disposed off
}

string name = description.FontName;

```