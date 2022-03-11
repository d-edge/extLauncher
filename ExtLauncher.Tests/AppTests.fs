module ExtLauncher.AppTests

open Swensen.Unquote
open Xunit

[<Fact>]
let ``should load a folder`` () =
    let folderPath = "/test"
    let pattern = "*.ext"
    let loadFiles _ _ =
        [| "/test/file2.ext", "file2"
           "/test/file1.ext", "file1" |]
    let folder =
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
    let loadFiles _ _ = Array.empty
    let folder =
        App.loadFolder loadFiles
            { Path = ""
              Pattern = Pattern.from "" false
              Launchers = Array.empty }
    folder =! None
