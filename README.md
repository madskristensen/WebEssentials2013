[Web Essentials](http://vswebessentials.com) for Visual Studio 2013
=================

Web Essentials extends Visual Studio with lots of new features that web developers have been missing for many years. 

If you ever write CSS, HTML, JavaScript, TypeScript, CoffeeScript, LiveScript or LESS, then you will find many useful features that make your life as a developer easier. 

This is for all Web developers using Visual Studio.

To get the latest nightly build, follow [these instructions](http://vswebessentials.com/download#nightly).


##Getting started
To contribute to this project, you'll need to do a few things first:

 1. Fork the project on GitHub
 1. Clone it to your computer
 1. Install the [Visual Studio 2013 SDK](http://www.microsoft.com/visualstudio/eng/downloads#d-vs-sdk).
 1. Open the solution in VS2013.

To install your local fork into your main VS instance, you will first need to open `Source.extension.vsixmanifest` and bump the version number to make it overwrite the (presumably) already-installed production copy. (alternatively, just uninstall Web Essentials from within VS first)

You can then build the project, then double-click the VSIX file from the bin folder to install it in Visual Studio.


##Useful Links
 - [Getting started with Visual Studio extension development](http://blog.slaks.net/2013-10-18/extending-visual-studio-part-1-getting-started/)
 - [About Web Essentials features](http://blogs.msdn.com/b/mvpawardprogram/archive/2013/11/05/making-web-development-wonderful-again-with-web-essentials.aspx)
 - [Inside the Visual Studio editor](http://msdn.microsoft.com/en-us/library/vstudio/dd885240.aspx)
 - [Extending the Visual Studio editor](http://msdn.microsoft.com/en-us/library/vstudio/dd885244.aspx)
