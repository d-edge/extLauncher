namespace extLauncher

open System

module IO =
    open System.IO
    open System.Text.RegularExpressions

    let [<Literal>] AppName = "extLauncher"

    let userPath =
        let path = Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.ApplicationData, AppName)
        Directory.CreateDirectory path |> ignore
        path

    let userPathCombine path =
        Path.Combine(userPath, path)

    let private enumerateFiles folderPath = function
        | WildcardPattern pattern ->
            Directory.EnumerateFiles(folderPath, pattern, EnumerationOptions(RecurseSubdirectories = true, IgnoreInaccessible = true, MatchType = MatchType.Simple))
        | RegexPattern pattern ->
            let regex = Regex pattern
            Directory.EnumerateFiles(folderPath, "*", EnumerationOptions(RecurseSubdirectories = true, IgnoreInaccessible = true, MatchType = MatchType.Simple))
            |> Seq.filter (Path.GetFileName >> regex.IsMatch)

    let getFiles folderPath pattern =
        enumerateFiles folderPath pattern
        |> Seq.map (fun path -> path, Path.GetFileNameWithoutExtension path)
        |> Seq.toArray

module Db =
    open LiteDB

    BsonMapper.Global.Entity<Folder>().DbRef(fun f -> f.Files) |> ignore

    let dbPath = IO.userPathCombine $"%s{IO.AppName}.db"
    let newReadOnlyDb () = new LiteDatabase($"Filename=%s{dbPath}; Mode=ReadOnly")
    let newSharedDb () = new LiteDatabase($"Filename=%s{dbPath}; Mode=Shared")

    let findFolder (path: string) =
        use db = newReadOnlyDb ()
        let doc = db.GetCollection<Folder>().Include(fun f -> f.Files).FindById path
        if box doc <> null then Some doc else None

    let updateFile (file: File) =
        use db = newSharedDb ()
        db.GetCollection<File>().Update file |> ignore
        file

    let deleteFolder path =
        match findFolder path with
        | None -> ()
        | Some folder ->
            use db = newSharedDb ()
            for file in folder.Files do
                db.GetCollection<File>().Delete file.Id |> ignore
            db.GetCollection<Folder>().Delete folder.Id |> ignore

    let upsertFolder (folder: Folder) =
        deleteFolder folder.Id
        use db = newSharedDb ()
        db.GetCollection<File>().InsertBulk folder.Files |> ignore
        db.GetCollection<Folder>().Insert folder |> ignore
        folder
