# Agent.Zorge.Moq

Visual Studio extension and Roslyn analyzer that helps to write unit tests using [Moq](https://github.com/moq/moq4) mocking library. Migration of [AgentZorge](https://github.com/Litee/AgentZorge) Resharper plugin to Roslyn.

Works in Visual Studio 2017 only because in previous versions Roslyn completion service is not available.

## Supported features

### Suggest It.IsAny() when setting up mocked method, including full set of arguments for accepting any parameters

![](https://github.com/Litee/Agent.Zorge.Moq/blob/master/media/suggest-isany-argument.png)

#### Generate lambdas for Callback() and Returns() methods

![](https://github.com/Litee/Agent.Zorge.Moq/blob/master/media/suggest-callback-argument.png)

#### Suggest variable names based on the name of mocked interface

![](https://github.com/Litee/Agent.Zorge.Moq/blob/master/media/variable-name-suggestion.png)

#### Moq: Suggest existing mocks if they are available, otherwise suggest *new Mock().Object*

![](https://github.com/Litee/Agent.Zorge.Moq/blob/master/media/suggest-existing-mocks.png)


### Highlight callbacks with invalid number of arguments or incompatible argument types

![](https://github.com/Litee/Agent.Zorge.Moq/blob/master/media/highlight-incompatible-callbacks.png)

## How to install:

* (Option 1) Install "Agent.Zorge.Moq" NuGet package into test projects. Con: Extension will work for specific projects only. Pro: It will be available for all project developers automatically.
* (Option 2) Install "Agent.Zorge.Moq" extension into Visual Studio. Con: Every developer must install extension manually. Pro: It works for all your projects. *Option is temporary unavailable because of problems at Visual Studio Market web site.*
