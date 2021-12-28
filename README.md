# tsfilewatcher

Skip the build step altogether if you're just making TypeScript changes. Stole most of the code from this [FileSystemWatcher example](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-6.0#examples).

1. `cd` to the directory of this project. Enter `dotnet run`
1. Now in another console, `cd` to your `UI-2/` directory, and run `tsc -w -p .`. After you do that, any time you save a TypeScript file, your TS will recompile and your localhost will update with the latest build of `brightmetrics.js`
