namespace extLauncher

open System
open System.ComponentModel
open Spectre.Console
open Spectre.Console.Cli

[<AutoOpen>]
module private Implementations =
    open System.Diagnostics
    type Path = System.IO.Path

    let markup value = AnsiConsole.MarkupLine value

    let notInitialized () =
        markup $"Folder not yet indexed: [yellow]%s{IO.AppName}[/] index [gray]--help[/]"
        1

    let run (file: File) launcher =
        markup $"""Launching [green]%s{file.Name.value}[/] [gray]%s{file.Path.value}[/]..."""
        let file = file |> File.triggered |> Db.updateFile

        match launcher with
        | None ->
            let psi = ProcessStartInfo file.Path.value
            psi.UseShellExecute <- true
            Process.Start psi |> ignore
        | Some launcher ->
            let path =
                match launcher.Choose with
                | Choose.File -> file.Path.value
                | Choose.Directory -> Path.GetDirectoryName file.Path.value
                | _ -> NotImplementedException() |> raise

            let psi = ProcessStartInfo launcher.Path.value
            psi.Arguments <- Launcher.buildArgs launcher path
            Process.Start psi |> ignore

    let chooseLauncher folder file =
        match folder.Launchers with
        | [||] -> run file None
        | [| launcher |] -> run file (Some launcher)
        | launchers ->
            Helpers.searchByName launchers (fun l -> l.Name)
            |> Console.prompt Console.Terminal "With which launcher?" Launcher.name 10
            |> function
                | Some launcher -> run file (Some launcher)
                | None -> ()

    let filePrompt (file: File) : string =
        $"""[white]%s{file.Name.value}[/]  [gray](%s{file.Path.folder.value})[/]"""

    let prompt folder =
        folder
        |> App.makeSearcher
        |> Console.prompt Console.Terminal "Search and launch:" filePrompt 10
        |> Option.iter (chooseLauncher folder)

    let withLoader<'T> (worker: StatusContext -> 'T) =
        AnsiConsole.Status().Start("Indexing...", worker)

    let currentPath = FolderPath Environment.CurrentDirectory

    let findFolder () =
        let rec find path =
            match Db.findFolder path with
            | Some f -> Some f
            | None ->
                let parentFolder =
                    (path.value |> Path.GetDirectoryName |> Option.ofObj |> (Option.map FolderPath))

                match parentFolder with
                | None -> None
                | Some p -> find p

        find currentPath

    let toCount str num =
        if num > 1 then $"%i{num} %s{str}s" else $"%i{num} %s{str}"

    let noNull s = if isNull s then "" else s

    let printLaunchers folder =
        let launchers =
            Table()
                .AddColumns(
                    [|
                        "Name"
                        "Choose"
                        "Path"
                        "Arguments"
                    |]
                )

        launchers.Border <- TableBorder.Minimal

        for l in folder.Launchers do
            launchers.AddRow(
                [|
                    l.Name
                    string l.Choose
                    l.Path.value
                    noNull l.Arguments
                |]
            )
            |> ignore

        AnsiConsole.Write launchers

type PromptCommand() =
    inherit Command()

    override _.Execute c =
        findFolder ()
        |> Option.map (prompt >> fun () -> 0)
        |> Option.defaultWith notInitialized

type IndexSettings() =
    inherit CommandSettings()

    [<CommandArgument(0, "<pattern>")>]
    [<Description "The search string to match against the file names.">]
    member val Pattern = "" with get, set

    [<CommandOption "-r|--regex">]
    [<Description "If set then the pattern is a regular expression, otherwise it's a combination of valid literal path and wildcard (* and ?) characters.">]
    member val IsRegex = false with get, set

type IndexCommand() =
    inherit Command<IndexSettings>()

    override _.Execute(_, settings) =
        fun _ ->
            App.index IO.getFiles Db.upsertFolder {
                Path = currentPath
                Pattern = Pattern.init settings.Pattern settings.IsRegex
                Launchers = Array.empty
            }
        |> withLoader
        |> function
            | Some folder ->
                printfn $"""%s{toCount "file" folder.Files.Length} indexed."""
                markup $"Start to search and launch: [yellow]%s{IO.AppName}[/]"
                markup $"Add a specific launcher: [yellow]%s{IO.AppName}[/] launcher [gray]--help[/]"
                0
            | None ->
                printfn $"%s{Console.NoMatch}"
                -1

type LauncherSettings() =
    inherit CommandSettings()

    [<CommandArgument(0, "<name>")>]
    [<Description "Name of the launcher.">]
    member val Name = "" with get, set

type SetLauncherSettings() =
    inherit LauncherSettings()

    [<CommandArgument(0, "<path>")>]
    [<Description "Launcher full path or launcher filename in the env path.">]
    member val Path = "" with get, set

    [<CommandOption "-a|--args">]
    [<Description "Launcher command line arguments.">]
    member val Arguments = "" with get, set

    [<CommandOption "-c|--choose">]
    [<Description "Which should be launched, the 'file' [italic](default)[/] or the 'directory'?">]
    member val Choose = Choose.File with get, set

type RemoveLauncherSettings() =
    inherit LauncherSettings()

type SetLauncherCommand() =
    inherit Command<SetLauncherSettings>()

    override _.Execute(_, settings) =
        match findFolder () with
        | None -> notInitialized ()
        | Some folder ->
            markup $"[teal]%s{settings.Name}[/] launcher updated."

            {
                Name = settings.Name
                Path = FilePath settings.Path
                Arguments = settings.Arguments
                Choose = settings.Choose
            }
            |> fun launcher ->
                match folder.Launchers |> Array.tryFindIndex (fun l -> l.Name = launcher.Name) with
                | Some index ->
                    folder.Launchers.[index] <- launcher
                    folder
                | None -> { folder with Launchers = Array.insertAt 0 launcher folder.Launchers }
            |> Db.upsertFolder
            |> printLaunchers

            0

    interface ICommandLimiter<LauncherSettings>

type RemoveLauncherCommand() =
    inherit Command<RemoveLauncherSettings>()

    override _.Execute(_, settings) =
        match findFolder () with
        | None -> notInitialized ()
        | Some folder ->
            match folder.Launchers |> Array.tryFindIndex (fun l -> l.Name = settings.Name) with
            | Some index ->
                markup $"[green]%s{settings.Name}[/] launcher removed."

                { folder with Launchers = Array.removeAt index folder.Launchers }
                |> Db.upsertFolder
                |> printLaunchers

                0
            | None ->
                markup $"[green]%s{settings.Name}[/] launcher not found."
                printLaunchers folder
                0

    interface ICommandLimiter<LauncherSettings>

type DeindexCommand() =
    inherit Command()

    override _.Execute _ =
        match Db.findFolder currentPath with
        | None -> notInitialized ()
        | Some folder ->
            Db.deleteFolder folder.Path
            printfn "Deindexed"
            0

type InfoCommand() =
    inherit Command()

    override _.Execute _ =
        match findFolder () with
        | None -> notInitialized ()
        | Some folder ->
            markup $"[teal]Path:[/]\n  %s{folder.Path.value.EscapeMarkup()}"

            markup $"\n[teal]Pattern:[/]\n  %s{folder.Pattern.value.EscapeMarkup()}"

            markup "\n[teal]Launchers:[/]"

            if Array.isEmpty folder.Launchers then
                printfn "  -\n"
            else
                printLaunchers folder

            markup "[teal]Indexed files:[/]"

            let files =
                Table()
                    .AddColumns(
                        [|
                            "Name"
                            "Triggered"
                            "Path"
                        |]
                    )

            files.Border <- TableBorder.Minimal

            for f in folder.Files do
                let path = f.Path.value.Remove(0, folder.Path.value.Length)

                files.AddRow(
                    [|
                        f.Name.value
                        string f.Triggered
                        path
                    |]
                )
                |> ignore

            AnsiConsole.Write files

            0

type RefreshCommand() =
    inherit Command()

    override _.Execute _ =
        match findFolder () with
        | None -> notInitialized ()
        | Some folder ->
            fun _ -> folder |> App.refresh IO.getFiles Db.upsertFolder
            |> withLoader
            |> Option.iter prompt

            0

module Program =

    [<EntryPoint>]
    let main args =
        let app = CommandApp<PromptCommand>()

        app.Configure(fun conf ->
            conf.SetApplicationName(IO.AppName) |> ignore

            conf
                .AddCommand<PromptCommand>("prompt")
                .WithDescription(
                    "[italic](default command)[/] Type to search. Arrows Up/Down to navigate. Enter to launch. Escape to quit."
                )
            |> ignore

            conf
                .AddCommand<IndexCommand>("index")
                .WithDescription(
                    "Indexes all files recursively with a specific pattern which can be a wildcard [italic](default)[/] or a regular expression."
                )
            |> ignore

            conf.AddBranch<LauncherSettings>(
                "launcher",
                fun launcher ->
                    launcher.SetDescription("Add, update or remove a launcher [italic](optional)[/].")

                    launcher
                        .AddCommand<SetLauncherCommand>("set")
                        .WithDescription("Add or update a launcher.")
                    |> ignore

                    launcher
                        .AddCommand<RemoveLauncherCommand>("remove")
                        .WithDescription("Remove a launcher.")
                    |> ignore
            )
            |> ignore

            conf
                .AddCommand<DeindexCommand>("deindex")
                .WithDescription("Clears the current index.")
            |> ignore

            conf
                .AddCommand<InfoCommand>("info")
                .WithDescription("Prints the current pattern and all the indexed files.")
            |> ignore

            conf
                .AddCommand<RefreshCommand>("refresh")
                .WithDescription("Updates the current index.")
            |> ignore

            conf.AddExample(
                [|
                    "index"
                    "*.sln"
                |]
            )

            conf.AddExample(
                [|
                    "index"
                    "\"(.*)[.](fs|cs)proj$\""
                    "--regex"
                |]
            )

            conf.AddExample(
                [|
                    "launcher"
                    "mylauncher"
                    "set"
                    "execpath"
                |]
            )

            conf.AddExample(
                [|
                    "launcher"
                    "mylauncher"
                    "remove"
                |]
            )

            conf.AddExample(
                [|
                    "launcher"
                    "vscode"
                    "set"
                    "/usr/bin/code"
                    "--choose"
                    "file"
                    "--args=\"-r %s\""
                |]
            )

            conf.AddExample(
                [|
                    "launcher"
                    "vscode"
                    "set"
                    @"""$env:LOCALAPPDATA\Programs\Microsoft VS Code\bin\code.cmd"""
                    "--choose"
                    "directory"
                |]
            )

            conf.AddExample(
                [|
                    "launcher"
                    "explorer"
                    "set"
                    "explorer.exe"
                    "--choose"
                    "directory"
                |]
            )

#if DEBUG
            conf.ValidateExamples() |> ignore
#endif
        )

        app.Run args
