
NLua
=============

| NuGet | NuGet (Pre-Release) |
| ------|------|
|[![nuget](https://img.shields.io/nuget/v/NLua.svg)](https://www.nuget.org/packages/NLua)|[![nuget](https://img.shields.io/nuget/vpre/NLua.svg)](https://www.nuget.org/packages/NLua)|

|  | Status | 
| :------ | :------: | 
| **Linux**   | [![Linux](https://travis-ci.org/nlua/NLua.svg?branch=master)](https://travis-ci.org/nlua/NLua) |
| **AppVeyor** | [![Build status](https://ci.appveyor.com/api/projects/status/jkqcy9m9k35jwolx?svg=true)](https://ci.appveyor.com/project/nlua/NLua)|
|**Mac (VSTS)** | [![Build Status](https://nlua.visualstudio.com/_apis/public/build/definitions/4f3cd26e-b8f7-4e52-9e78-c2ecf3de2929/1/badge)](https://nlua.visualstudio.com/nlua/_build/index?context=mine&path=%5C&definitionId=1&_a=completed) |
|**Windows (VSTS)** | [![Build Status](https://nlua.visualstudio.com/_apis/public/build/definitions/4f3cd26e-b8f7-4e52-9e78-c2ecf3de2929/2/badge)](https://nlua.visualstudio.com/nlua/_build/index?context=mine&path=%5C&definitionId=2&_a=completed) |


This library will help you to create simple Mazes.

    using NLua.Core;

    Maze maze = Creator.GetCreator ().Create (10, 10);

This will create a maze 10 x 10:

    ┌───────────┬─┬─┬───┐ 
    │ ┌───┬─╴ ╷ │ │ ╵ ╷ │ 
    ├─┘ ╷ ╵ ┌─┤ │ └───┤ │ 
    │ ┌─┴───┘ ╵ ├─┬─╴ │ │ 
    │ └─┐ ╶─┬───┤ │ ╶─┘ │ 
    │ ╷ └─┐ ╵ ╷ ╵ └───┐ │ 
    ├─┴─╴ │ ┌─┴───┬───┘ │ 
    │ ╶───┤ │ ╶─┐ │ ╶───┤ 
    │ ┌─╴ ├─┴─┐ │ └───┐ │ 
    │ │ ╶─┘ ╷ ╵ └─┐ ╶─┘ │ 
    └─┴─────┴─────┴─────┘ 

To get cell information you can do:

    // maze [line, column]
    Cell cell = maze [0, 1];
    if (cell.HasRightWall) {
        ...
    }
    if (cell.HasLeftWall) {
        ...
    }
    if (cell.HasBottomWall) {
        ...
    }
    if (cell.HasTopWall) {
        ...
    }


Building
--------

    msbuild


Kruskal
--------

To create a maze using Kruskal algorithm use:

    Maze maze = Creator.GetCreator (Algorithm.Kruskal).Create (10, 10)

This will create a maze like:

    ┌─┬───┬─────┬───┬───┐ 
    │ ╵ ╶─┤ ╷ ╶─┴─╴ │ ╷ │ 
    ├─╴ ╶─┘ │ ╷ ┌─╴ ╵ └─┤ 
    ├─╴ ╷ ╷ └─┼─┴─┬───┬─┤ 
    │ ╶─┼─┤ ┌─┘ ╷ ├─╴ │ │ 
    │ ╷ │ ╵ │ ╷ │ └─┐ │ │ 
    ├─┴─┘ ╷ └─┘ │ ╶─┤ │ │ 
    ├───╴ ├─╴ ┌─┴───┘ ╵ │ 
    ├─┬─┐ │ ╷ └─╴ ╶─┬─╴ │ 
    │ ╵ └─┘ │ ╷ ┌───┘ ╶─┤ 
    └───────┴─┴─┴───────┘ 


Prim
--------

To create a maze using Prim algorithm use:

    Maze maze = Creator.GetCreator (Algorithm.Prim).Create (10, 10)

This will create a maze like:


    ┌─┬─┬─────┬─┬─────┬─┐ 
    │ ╵ ╵ ╷ ╷ ╵ ╵ ╶───┘ │ 
    │ ┌─╴ │ │ ╶─┐ ╶─┐ ┌─┤ 
    │ │ ╷ └─┤ ┌─┘ ╶─┴─┘ │ 
    │ └─┼─╴ ├─┘ ╷ ╶─┐ ╷ │ 
    │ ╷ │ ╷ ├─╴ │ ╷ ├─┤ │ 
    ├─┘ ├─┘ └─┐ └─┴─┘ │ │ 
    │ ╷ ├─┬───┘ ┌─╴ ╷ └─┤ 
    │ │ │ ╵ ╶─┐ └─┐ │ ╷ │ 
    │ └─┼─╴ ╷ ├───┴─┘ └─┤ 
    └───┴───┴─┴─────────┘  

