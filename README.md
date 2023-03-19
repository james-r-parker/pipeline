# dotnethelp Pipeline

[![Workflow](https://github.com/james-r-parker/pipeline/actions/workflows/build.yml/badge.svg)](https://github.com/james-r-parker/pipeline/actions)
[![Branch Coverage](https://james-r-parker.github.io/pipeline/badge_branchcoverage.svg)](https://james-r-parker.github.io/pipeline/)
[![Line Coverage](https://james-r-parker.github.io/pipeline/badge_linecoverage.svg)](https://james-r-parker.github.io/pipeline/)
[![Method Coverage](https://james-r-parker.github.io/pipeline/badge_methodcoverage.svg)](https://james-r-parker.github.io/pipeline/)
[![NuGet Download](https://img.shields.io/nuget/dt/DotNetHelp.Pipeline?label=NuGet%20Downloads)](https://www.nuget.org/packages/DotNetHelp.Pipeline)

A simple pipeline builder for C#

## Install
```dotnet add package DotNetHelp.Pipeline```

## Examples

### Simple

A simple pipeline with two steps executed with a single input item. Each step is executed by passing in a [PipelineRequest](https://github.com/james-r-parker/pipeline/blob/main/src/DotNetHelp.Pipelines/Application/PipelineRequest.cs). A pipeline request has access to both the item being processed and a shared global context which is shared across all request to the pipeline. A [Context](https://github.com/james-r-parker/pipeline/blob/main/src/DotNetHelp.Pipelines/Application/Context.cs) allows you to store and retieve objects stored within each step.

``` csharp
using DotNetHelp.Pipelines;

var  _cancellationToken = new CancellationToken();
var _builder = new PipelineBuilder();

_builder
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(1));
                return Task.CompletedTask;
        })
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(2));
                return Task.CompletedTask;
        });

var _pipeline = _builder.Build(_cancellationToken);
var input = new SourceData();
Context result = await _pipeline.InvokeSync(input);
```

### Multiple Input

``` csharp
using DotNetHelp.Pipelines;

var  _cancellationToken = new CancellationToken();
var _builder = new PipelineBuilder();

_builder
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(1));
                return Task.CompletedTask;
        })
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(2));
                return Task.CompletedTask;
        });

var _pipeline = _builder.Build(_cancellationToken);
var input = new List<SourceData> { new SourceData(1), new SourceData(2) };
Context result = await _pipeline.InvokeMany(input);
```

### Async Input

``` csharp
using DotNetHelp.Pipelines;

var  _cancellationToken = new CancellationToken();
var _builder = new PipelineBuilder();

_builder
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(1));
                return Task.CompletedTask;
        })
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(2));
                return Task.CompletedTask;
        });

var _pipeline = _builder.Build(_cancellationToken);
await _builder.Invoke();

foreach (var input in inputs)
{
        await _pipeline.AddInput<T>(input);
}

await Finalise();

await foreach (var item in Result.WithCancellation(_cancellationToken.Token))
{
        yield return item;
}

```

### Filtering

A filter will allow the request to continue to following steps if the filter returns true. If the filter returns false the context will be discarded and no futher steps will be executed for this input.

``` csharp
using DotNetHelp.Pipelines;

var  _cancellationToken = new CancellationToken();
var _builder = new PipelineBuilder();

_builder
        .AddInlineFilterStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data) && data.Id % 2 == 0)
                {
                        return Task.FromResult(false);
                }

                return Task.FromResult(false);
        })
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(2));
                return Task.CompletedTask;
        });

var _pipeline = _builder.Build(_cancellationToken);
var input = new SourceData();
Context result = await _pipeline.InvokeSync(input);
```

### Branching

 A branch is a sub pipeline that once completed will continue back to parent pipeline. The branch will only be executed if the filter returns true. If the filter returns false the pipeline will continue to the next steps without executing the branch sub pipeline.

 ``` csharp
using DotNetHelp.Pipelines;

var  _cancellationToken = new CancellationToken();
var _builder = new PipelineBuilder();

_builder
        .AddInlineFilterStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data) && data.Id % 2 == 0)
                {
                        return Task.FromResult(false);
                }

                return Task.FromResult(false);
        })
        .AddInlineBranchStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data) && data.Id % 2 == 0)
                {
                        return Task.FromResult(true);
                }
                return Task.FromResult(false);
        },
        (b) =>
        {
                b
                .AddInlineStep((r) =>
                {
                        if (r.Item.TryGetValue(out SourceData data))
                        {
                                data.Increment(data.Id);
                        }
                        return Task.CompletedTask;
                });
        })
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(2));
                return Task.CompletedTask;
        });

var _pipeline = _builder.Build(_cancellationToken);
var input = new SourceData();
Context result = await _pipeline.InvokeSync(input);
```

### Forking
A fork is a sub pipeline that once completed will return its output. If a fork is triggered no further pipeline steps will be executed for this request. The fork will only be executed if the filter returns true. If the filter returns false the pipeline will continue to the next steps without executing the fork sub pipeline.

 ``` csharp
using DotNetHelp.Pipelines;

var  _cancellationToken = new CancellationToken();
var _builder = new PipelineBuilder();

_builder
        .AddInlineFilterStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data) && data.Id % 2 == 0)
                {
                        return Task.FromResult(false);
                }

                return Task.FromResult(false);
        })
        .AddInlineForkStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data) && data.Id % 2 == 0)
                {
                        return Task.FromResult(true);
                }
                return Task.FromResult(false);
        },
        (b) =>
        {
                b
                .AddInlineStep((r) =>
                {
                        if (r.Item.TryGetValue(out SourceData data))
                        {
                                data.Increment(data.Id);
                        }
                        return Task.CompletedTask;
                });
        })
        .AddInlineStep((r) =>
        {
                if (r.Item.TryGetValue(out SourceData data))
                {
                        data.Increment(data.Id);
                }
                r.Context.Add(new Data(2));
                return Task.CompletedTask;
        });

var _pipeline = _builder.Build(_cancellationToken);
var input = new SourceData();
Context result = await _pipeline.InvokeSync(input);
```