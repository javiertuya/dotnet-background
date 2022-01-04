![Status](https://github.com/javiertuya/dotnet-background/actions/workflows/test.yml/badge.svg)
[![Nuget](https://img.shields.io/nuget/v/DotnetTestSplit)](https://www.nuget.org/packages/DotnetBackground/)

# dotnet-test-split

An easy way to launch dotnet run background processes and kill each process and their descendants with a single command.

## Usage

Install as a [.Net Core tool](https://docs.microsoft.com/es-es/dotnet/core/tools/dotnet-tool-install):
```
dotnet tool install --global DotnetBackground
```

For each project to be launched, run the tool as if it were run using `dotnet` command. 
This example launches two projects, with some dotnet options and arguments passed to the second process:

```
DotnetBackground run --project project1/project1.csproj
DotnetBackground run --project project2/project2.csproj --no-restore param1 param2
```

All projects will run in the background under a shell (cmd on windows or bash on unix), producing the following files:

- `dotnet-projectN-out.log`: the standard output of each project N
- `dotnet-projectN-err.log`: the standard error output of each project N
- `dotnet-background-pids.log`: stores the PID of each process to enable kill all processes with a single command

To kill all processes, use a single kill command. If any process has child processes, they will be killed too:

```
DotnetBackground kill
```

## Additional options

`DotnetBackground` allows some additional options that can be added to the command line (these parameters are not send to `dotnet`)

- `--out <path>`: Sets the output folder where the log files are stored. Default is the current folder.
- `--name <name>`: Sets the name of the process (used to identfy the log files). By default it is the name of the project. 
Note that if several instances of the the same project are launched, they need to specify different names.

This example launches two instances of the same process with different arguments and moves all logs to the reports folder:

```
DotnetBackground run --project project1/project1.csproj --name instance1 --out report --no-restore param1
DotnetBackground run --project project1/project1.csproj --name instance2 --out report --no-restore param2
```

Note that it is not required the projects being previously built, but is recommendable do so when launching multiple instances of the same project to avoid interferences during startup.