# dotnethelp Pipeline

[![Workflow](https://github.com/james-r-parker/pipeline/actions/workflows/build.yml/badge.svg)](https://github.com/james-r-parker/pipeline/actions)
[![Branch Coverage](https://james-r-parker.github.io/pipeline/badge_branchcoverage.svg)](https://james-r-parker.github.io/pipeline/)
[![Line Coverage](https://james-r-parker.github.io/pipeline/badge_linecoverage.svg)](https://james-r-parker.github.io/pipeline/)
[![Method Coverage](https://james-r-parker.github.io/pipeline/badge_methodcoverage.svg)](https://james-r-parker.github.io/pipeline/)
[![NuGet Download](https://img.shields.io/nuget/dt/DotNetHelp.Pipeline?label=NuGet%20Downloads)](https://www.nuget.org/packages/DotNetHelp.Pipeline)

A simple pipeline builder for C#

## Install
```dotnet add package DotNetHelp.Pipeline```

## Example

``` csharp
var  _cancellationToken = new CancellationToken();
var _builder = new PipelineBuilder();

_builder
        .ConfigureServices(services =>
        {
        })
        .AddInlineStep((r) =>
        {
                return Task.CompletedTask;
        })
        .AddInlineStep((r) =>
        {
                return Task.CompletedTask;
        });

var _pipeline = _builder.Build(_cancellationToken);
var input = new T();
Context result = await _pipeline.InvokeSync(input);
```