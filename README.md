![sharpconfig_logo.png](sharpconfig_logo.png)

This is a PCL version of SharpConfig. File interaction deleted, only Stream based left. Not properly tested Object serializing/deserialising, it used only Instance and Public Properties/Fields, now it should parse any. Should be fixed.

SharpConfig is an easy-to-use CFG/INI configuration library for .NET.

You can use SharpConfig in your .NET applications to add the functionality
to read, modify and save configuration files and streams, in either text or binary format.
The library is backward compatible up to .NET 2.0.

> If SharpConfig has helped you and you feel like donating, [feel free](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=WWN94LMDN5HMC)!
> Donations help to keep the development of SharpConfig active.

Installing via NuGet
---
You can install SharpConfig via the following NuGet command:
> Install-Package sharpconfig

[NuGet Page](https://www.nuget.org/packages/sharpconfig/)



An example Configuration
---

```cfg
[General]
# a comment
SomeString = Hello World!
SomeInteger = 10 # an inline comment
SomeFloat = 20.05
ABoolean = true
```

To read these values, your C# code would look like:
```csharp
Configuration config = Configuration.LoadFromFile("sample.cfg");
Section section = config["General"];

string someString = section["SomeString"].StringValue;
int someInteger = section["SomeInteger"].IntValue;
float someFloat = section["SomeFloat"].FloatValue.
```

Iterating through a Configuration
---

```csharp
foreach (var section in myConfig)
{
    foreach (var setting in section)
    {
        // ...
    }
}
```

Creating a Configuration in-memory
---

```csharp
// Create the configuration.
var myConfig = new Configuration();

// Set some values.
// This will automatically create the sections and settings.
myConfig["Video"]["Width"].IntValue = 1920;
myConfig["Video"]["Height"].IntValue = 1080;

// Set an array value.
myConfig["Video"]["Formats"].SetValue( new string[] { "RGB32", "RGBA32" } );

// Get the values just to test.
int width = myConfig["Video"]["Width"].IntValue;
int height = myConfig["Video"]["Height"].IntValue;
string[] formats = myConfig["Video"]["Formats"].GetValueArray<string>();
// ...
```

Saving a Configuration
---

```csharp
myConfig.SaveToStream(myStream);            // Save to a text-based stream.
myConfig.SaveToBinaryStream(myStream);      // Save to a binary stream.
```

More
---
SharpConfig has more features, such as support for **arrays**, **enums** and **object mapping**.
For details and examples, please visit the [Wiki](https://github.com/cemdervis/SharpConfig/wiki).
There are also use case examples available in the [Example File](https://github.com/cemdervis/SharpConfig/blob/master/Example/Program.cs).
