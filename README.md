[Web Essentials](http://vswebessentials.com) for Visual Studio 2013 Preview
=================

Web Essentials extends Visual Studio with lots of new features that web developers have been missing for many years. 

If you ever write CSS, HTML, JavaScript, TypeScript, CoffeeScript or LESS, then you will find many useful features that make your life as a developer easier. 

This is for all Web developers using Visual Studio.


##Getting started
To contribute to this project, you'll need to do a few things first:

 1. Fork the project on GitHub
 1. Clone it to your computer
 1. Install the [Visual Studio 2013 SDK](http://www.microsoft.com/visualstudio/eng/2013-downloads#d-additional-software).
 1. Open the solution in VS2013.
 1. Open Project Properties, go to the Debug tab, and select `Start External Program`, then select the Visual Studio 2013 devenv.exe (typically `C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\devenv.exe`)  
  Then, enter `/rootsuffix Exp` in Command line arguments to make VS launch the experimental hive.  (this will automatically load the debug copy of the addin without messing up your primary VS instance)

To install your local fork into your main VS instance, you will first need to open `Source.extension.vsixmanifest` and bump the version number to make it overwrite the (presumably) already-installed production copy.

You can then build the project, then double-click the VSIX file from the bin folder to install it in Visual Studio.