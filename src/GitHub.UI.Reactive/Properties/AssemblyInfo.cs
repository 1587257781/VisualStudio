﻿using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

[assembly: AssemblyTitle("GitHub.UI.Reactive")]
[assembly: AssemblyDescription("GitHub flavored WPF styles and controls that require Rx and RxUI")]
[assembly: Guid("885a491c-1d13-49e7-baa6-d61f424befcb")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page, 
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page, 
                                              // app, or any theme specific resource dictionaries)
    )]

[assembly: XmlnsDefinition("https://github.com/github/VisualStudio", "GitHub.UI")]
