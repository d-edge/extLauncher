namespace extLauncher

open System

module IO =
    open System.IO
    open System.Text.RegularExpressions

    [<Literal>]
    let AppName = "extLauncher"

    let userPath =
        let path =
            Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.ApplicationData, AppName)

        Directory.CreateDirectory path |> ignore
        path

    let userPathCombine path = Path.Combine(userPath, path)

    let private enumerateFiles (FolderPath path) =
        function
        | WildcardPattern pattern ->
            Directory.EnumerateFiles(
                path,
                pattern,
                EnumerationOptions(
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MatchType = MatchType.Simple
                )
            )
        | RegexPattern pattern ->
            let regex = Regex pattern

            Directory.EnumerateFiles(
                path,
                "*",
                EnumerationOptions(
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MatchType = MatchType.Simple
                )
            )
            |> Seq.filter (Path.GetFileName >> regex.IsMatch)

    let getFiles folderPath pattern =
        enumerateFiles folderPath pattern
        |> Seq.map (fun path -> FilePath path, path |> Path.GetFileNameWithoutExtension |> FileName)
        |> Seq.toArray

module Db =

    type LauncherDb = {
        Name: string
        Path: string
        Arguments: string
        Choose: int
    } with

        static member fromDomain(launcher: Launcher) = {
            Name = launcher.Name
            Path = launcher.Path.value
            Arguments = launcher.Arguments
            Choose = int launcher.Choose
        }

        static member toDomain(launcherDb: LauncherDb) : Launcher = {
            Name = launcherDb.Name
            Path = FilePath launcherDb.Path
            Arguments = launcherDb.Arguments
            Choose = Choose.init launcherDb.Choose
        }

    type FileDb = {
        Id: string
        Name: string
        Triggered: int32
    } with

        static member fromDomain(file: File) = {
            Id = file.Path.value
            Name = file.Name.value
            Triggered = file.Triggered
        }

        static member toDomain(fileDb: FileDb) = {
            Path = FilePath fileDb.Id
            Name = FileName fileDb.Name
            Triggered = fileDb.Triggered
        }

    // Should be serializable to BSON
    [<CLIMutable>]
    type FolderDb = {
        Id: string
        Pattern: string
        IsRegex: bool
        Launchers: LauncherDb array
        Files: FileDb array
    } with

        static member fromDomain(folder: Folder) = {
            Id = folder.Path.value
            Pattern = folder.Pattern.value
            IsRegex = folder.Pattern.isRegex
            Launchers = folder.Launchers |> Array.map LauncherDb.fromDomain
            Files = folder.Files |> Array.map FileDb.fromDomain
        }

        static member toDomain(folderDb: FolderDb) = {
            Path = FolderPath folderDb.Id
            Pattern = Pattern.init folderDb.Pattern folderDb.IsRegex
            Launchers = folderDb.Launchers |> Array.map LauncherDb.toDomain
            Files = folderDb.Files |> Array.map FileDb.toDomain
        }

    open LiteDB

    BsonMapper.Global.Entity<FolderDb>().DbRef(fun f -> f.Files) |> ignore

    let dbPath = IO.userPathCombine $"%s{IO.AppName}.db"

    let newReadOnlyDb () =
        new LiteDatabase($"Filename=%s{dbPath}; Mode=ReadOnly")

    let newSharedDb () =
        new LiteDatabase($"Filename=%s{dbPath}; Mode=Shared")

    let private tryFindFolder (path: string) =
        use db = newReadOnlyDb ()
        let doc = db.GetCollection<FolderDb>().Include(fun f -> f.Files).FindById path
        if box doc <> null then doc |> Some else None

    let findFolder (FolderPath path) =
        path |> tryFindFolder |> Option.map FolderDb.toDomain

    let updateFile (file: File) =
        let fileDb = file |> FileDb.fromDomain
        use db = newSharedDb ()
        fileDb |> db.GetCollection<FileDb>().Update |> ignore
        file

    let deleteFolder (FolderPath path) =
        match tryFindFolder path with
        | None -> ()
        | Some folder ->
            use db = newSharedDb ()

            for file in folder.Files do
                db.GetCollection<FileDb>().Delete file.Id |> ignore

            db.GetCollection<FolderDb>().Delete folder.Id |> ignore

    let upsertFolder (folder: Folder) =
        let folderDb = FolderDb.fromDomain folder
        deleteFolder folder.Path
        use db = newSharedDb ()
        folderDb.Files |> db.GetCollection<FileDb>().InsertBulk |> ignore
        folderDb |> db.GetCollection<FolderDb>().Insert |> ignore
        folder
