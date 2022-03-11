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

let refresh loadFiles save delete folder =
    { Path = folder.Id
      Pattern = Pattern.from folder.Pattern folder.IsRegex
      Launchers = folder.Launchers }
    |> index loadFiles save
    |> Option.orElseWith (fun () ->
        delete folder.Id
        None)

let makeSearcher folder =
    Helpers.searchByName folder.Files
