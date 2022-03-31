module extLauncher.AppTests

open Swensen.Unquote
open Xunit

[<Fact>]
let ``should load a folder`` () =
    let folderPath = "/test"
    let pattern = "*.ext"
    let folder =
        let loadFiles _ _ =
            [| "/test/file2.ext", "file2"
               "/test/file1.ext", "file1" |]
        App.loadFolder loadFiles
            { Path = folderPath
              Pattern = Pattern.from pattern false
              Launchers = Array.empty }
    folder =! Some
        { Id = folderPath
          Pattern = pattern
          IsRegex = false
          Files =
            [| File.create "/test/file1.ext" "file1"
               File.create "/test/file2.ext" "file2" |]
          Launchers = Array.empty }

[<Fact>]
let ``should not load a folder if no result`` () =
    let folder =
        let loadFiles _ _ = Array.empty
        App.loadFolder loadFiles
            { Path = ""
              Pattern = Pattern.from "" false
              Launchers = Array.empty }
    folder =! None

[<Fact>]
let ``refresh should synchronize files`` () =
    let newFolder =
        let loadFiles _ _ =
            [| "file1", ""
               "file3", "" |]
        let save = id
        { Id = ""
          Pattern = ""
          IsRegex = false
          Files =
            [| File.create "file1" ""
               File.create "file2" "" |]
          Launchers = Array.empty }
        |> App.refresh loadFiles save
        |> Option.get

    newFolder.Files.[0].Id =! "file1"
    newFolder.Files.[1].Id =! "file3"

[<Fact>]
let ``refresh should keep triggers`` () =
    let newFolder =
        let loadFiles _ _ =
            [| "file1", ""
               "file2", "" |]
        let save = id
        { Id = ""
          Pattern = ""
          IsRegex = false
          Files =
            [| File.create "file1" "" |> File.triggered
               File.create "file2" "" |]
          Launchers = Array.empty }
        |> App.refresh loadFiles save
        |> Option.get

    newFolder.Files.[0].Triggered =! 1
    newFolder.Files.[1].Triggered =! 0
