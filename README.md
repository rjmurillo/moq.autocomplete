# Agent.Zorge.Moq

Visual Studio extension and Roslyn analyzer that helps to write unit tests using [Moq](https://github.com/moq/moq4) mocking library. Migration of [AgentZorge](https://github.com/Litee/AgentZorge) Resharper plugin to Roslyn.

Currently works in Visual Studio 2017 only, does not work in VS 2015 and older.

## Supported features

### Suggest It.IsAny() when setting up mocked method, including full set of arguments for accepting any parameters

![](https://github.com/Litee/Agent.Zorge.Moq/blob/master/media/suggest-isany-argument.png)

### Highlight callbacks with invalid number of arguments or incompatible argument types

![](https://github.com/Litee/Agent.Zorge.Moq/blob/master/media/highlight-incompatible-callbacks.png)

## How to install:

* (Option 1) Install "Agent.Zorge.Moq" extension into Visual Studio - this way extension will work for all your projects. !!! Not available at the moment because of internal technical problem at Visual Studio Market !!!
* (Option 2) Install "Agent.Zorge.Moq" NuGet package into test projects - this way extension will work for specific projects only