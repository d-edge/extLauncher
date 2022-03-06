namespace ExtLauncher

open System
open System.ComponentModel
open Spectre.Console
open Spectre.Console.Cli

[<AutoOpen>]
module private Helpers =
    open System.Diagnostics

    let prn value = AnsiConsole.MarkupLine value

    let notInitialized () =
        prn "Folder not yet indexed."
        prn $"  [yellow]{IO.AppName}[/] index [teal]<pattern>[/]"
        prn "For more information:"
        prn $"  [yellow]{IO.AppName}[/] --help"
        1

    let run (file: File) =
        let psi = ProcessStartInfo file.Id
        psi.UseShellExecute <- true
        Process.Start psi |> ignore

    let trigger =
        File.triggered >> Db.updateFile >> run

    let prompt folder =
        folder
        |> App.makeSearcher
        |> Console.prompt Console.Terminal 10
        |> Option.iter trigger

    let withLoader<'T> (worker: StatusContext -> 'T) =
        AnsiConsole.Status().Start("Indexing...", worker)

    let currentPath =
        Environment.CurrentDirectory

    let findFolder () =
        let rec find path =
            if isNull path
            then None
            else
                match Db.findFolder path with
                | Some f -> Some f
                | None   ->
                    find (System.IO.Path.GetDirectoryName path)
        find currentPath

type PromptCommand () =
    inherit Command ()
    override _.Execute c =
        findFolder ()
        |> Option.map (prompt >> fun () -> 0)
        |> Option.defaultWith notInitialized

type IndexSettings () =
    inherit CommandSettings ()
        [<CommandArgument(0, "<pattern>")>]
        [<Description("The search string to match against the names of files. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.")>]
        member val Pattern = "" with get, set

type IndexCommand () =
    inherit Command<IndexSettings> ()
    override _.Execute (_, settings) =
        fun _ ->
            App.index IO.getFiles Db.upsertFolder currentPath settings.Pattern
        |> withLoader
        |> Option.iter prompt
        0

type DeindexCommand () =
    inherit Command ()
    override _.Execute _ =
        match Db.findFolder currentPath with
        | None -> notInitialized ()
        | Some folder ->
            Db.deleteFolder folder.Id
            prn "Deindexed"
            0

type InfoCommand () =
    inherit Command ()
    override _.Execute _ =
        match findFolder () with
        | None -> notInitialized ()
        | Some folder ->
            prn $"[teal]Path:[/]\n  {folder.Id}"
            prn $"[teal]Pattern:[/]\n  {folder.Pattern}"
            prn $"[teal]Indexed files:[/]"
            for file in folder.Files do
                prn $"  {file.Name}"
            0

type RefreshCommand () =
    inherit Command ()
    override _.Execute _ =
        match findFolder () with
        | None -> notInitialized ()
        | Some folder ->
            fun _ ->
                App.refresh
                    IO.getFiles
                    Db.upsertFolder
                    Db.deleteFolder
                    folder.Id folder.Pattern
            |> withLoader
            |> Option.iter prompt
            0

module Program =

    [<EntryPoint>]
    let main args =
        let app = CommandApp<PromptCommand>()
        app.Configure (fun conf ->
            conf.SetApplicationName(IO.AppName) |> ignore

            conf.AddCommand<PromptCommand>("search")
                .WithDescription("(Default) Type to search. Arrows Up/Down to navigate. Enter to launch the file.") |> ignore
            conf.AddCommand<IndexCommand>("index")
                .WithDescription("Indexes all files recursively with a specific pattern.") |> ignore
            conf.AddCommand<DeindexCommand>("deindex")
                .WithDescription("Clears the current index.") |> ignore
            conf.AddCommand<InfoCommand>("info")
                .WithDescription("Prints the current pattern and all the indexed files.") |> ignore
            conf.AddCommand<RefreshCommand>("refresh")
                .WithDescription("Updates the current index.") |> ignore
        )
        app.Run args
