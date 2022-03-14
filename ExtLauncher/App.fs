module ExtLauncher.App

type FolderConf =
    { Path: string
      Pattern: Pattern
      Launchers: Launcher array }

let loadFolder loadFiles conf : Folder option =
    loadFiles conf.Path conf.Pattern
    |> Array.map ((<||) File.create)
    |> Array.sort
    |> function
    | [||] -> None
    | files ->
        { Id = conf.Path
          Pattern = Pattern.value conf.Pattern
          IsRegex = Pattern.isRegex conf.Pattern
          Launchers = conf.Launchers
          Files = files }
        |> Some

let index loadFiles save conf : Folder option =
    loadFolder loadFiles conf
    |> Option.map save

let refresh loadFiles save (folder: Folder) : Folder option =

    let newFiles =
        Pattern.from folder.Pattern folder.IsRegex
        |> loadFiles folder.Id
        |> Array.map ((<||) File.create)

    let currentFiles =
        folder.Files
        |> Array.map (fun f -> f.Id, f)
        |> Map

    newFiles
    |> Array.map (fun newFile ->
        match currentFiles.TryFind newFile.Id with
        | Some current -> { newFile with Triggered = current.Triggered }
        | None -> newFile)
    |> fun files -> { folder with Files = files }
    |> save
    |> Some

let makeSearcher folder str =
    Helpers.searchByName folder.Files str
    |> Array.sort
