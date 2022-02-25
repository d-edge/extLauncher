module ExtLauncher.App

let loadFolder loadFiles folderPath pattern : Folder option =
    loadFiles folderPath pattern
    |> Array.map ((<||) File.create)
    |> Array.sort
    |> function
    | [||] -> None
    | files ->
        { Id = folderPath
          Pattern = pattern
          Files = files
          OpenWith = Array.empty }
        |> Some

let index loadFiles save folderPath pattern : Folder option =
    loadFolder loadFiles folderPath pattern
    |> Option.map save

let refresh loadFiles save delete folderPath pattern =
    index loadFiles save folderPath pattern
    |> Option.orElseWith (fun () ->
        delete folderPath
        None)

let makeSearcher folder =
    File.search folder.Files
