module extLauncher.App

type FolderConf = {
    Path: FolderPath
    Pattern: Pattern
    Launchers: Launcher array
}

let loadFolder loadFiles conf : Folder option =
    loadFiles conf.Path conf.Pattern
    |> Array.map ((<||) File.create)
    |> Array.sort
    |> function
        | [||] -> None
        | files ->
            {
                Path = conf.Path
                Pattern = conf.Pattern
                Files = files
                Launchers = conf.Launchers
            }
            |> Some

let index loadFiles save conf : Folder option =
    loadFolder loadFiles conf |> Option.map save

let refresh loadFiles save (folder: Folder) : Folder option =

    let newFiles =
        folder.Pattern |> loadFiles folder.Path |> Array.map ((<||) File.create)

    let currentFiles = folder.Files |> Array.map (fun f -> f.Path, f) |> Map

    newFiles
    |> Array.map (fun newFile ->
        match currentFiles.TryFind newFile.Path with
        | Some current -> {
            newFile with
                Triggered = current.Triggered
          }
        | None -> newFile
    )
    |> fun files -> { folder with Files = files }
    |> save
    |> Some

let makeSearcher folder str =
    Helpers.searchByName folder.Files (fun f -> f.Name.value) str |> Array.sort
