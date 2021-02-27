#!/bin/sh

dotnet fable watch ./src --outDir ./fs.js.build --run webpack serve
