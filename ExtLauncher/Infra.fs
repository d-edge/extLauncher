namespace ExtLauncher

open System

module IO =
    open System.IO

    let [<Literal>] AppName = "extLauncher"

    let userPath =
        let path = Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.ApplicationData, AppName)
        Directory.CreateDirectory path |> ignore
        path

    let userPathCombine path =
        Path.Combine(userPath, path)

    let getFiles (folderPath: string) pattern =
        Directory.EnumerateFiles(folderPath, pattern, SearchOption.AllDirectories)
        |> Seq.map (fun path -> path, Path.GetFileNameWithoutExtension path)
        |> Seq.toArray

module Db =
    open LiteDB

    BsonMapper.Global.Entity<Folder>().DbRef(fun f -> f.Files) |> ignore

    let dbPath = IO.userPathCombine $"{IO.AppName}.db"
    let newReadOnlyDb () = new LiteDatabase($"Filename=%s{dbPath}; Mode=ReadOnly")
    let newSharedDb () = new LiteDatabase($"Filename=%s{dbPath}; Mode=Shared")

    let findFolder (path: string) =
        use db = newReadOnlyDb ()
        let foo = db.GetCollection<Folder>().FindAll() |> List.ofSeq
        let doc = db.GetCollection<Folder>().Include(fun f -> f.Files).FindById path
        if box doc <> null then Some doc else None

    let upsertFolder (folder: Folder) =
        use db = newSharedDb ()
        db.GetCollection<File>().Upsert folder.Files |> ignore
        db.GetCollection<Folder>().Upsert folder |> ignore
        folder

    let updateFile (file: File) =
        use db = newSharedDb ()
        db.GetCollection<File>().Update file |> ignore
        file

    let deleteFolder (path: string) =
        match findFolder path with
        | None -> ()
        | Some folder ->
            use db = newSharedDb ()
            for file in folder.Files do
                db.GetCollection<File>().Delete file.Id |> ignore
            db.GetCollection<Folder>().Delete folder.Id |> ignore
