module extLauncher.AppTests

open Swensen.Unquote
open Xunit

[<Fact>]
let ``should load a folder`` () =
    let folderPath = FolderPath "/test"
    let pattern = "*.ext"

    let folder =
        let loadFiles _ _ = [|
            FilePath "/test/file2.ext", FileName "file2"
            FilePath "/test/file1.ext", FileName "file1"
        |]

        App.loadFolder loadFiles {
            Path = folderPath
            Pattern = Pattern.init pattern false
            Launchers = Array.empty
        }

    folder
    =! Some {
        Path = folderPath
        Pattern = Pattern.init pattern false
        Files = [|
            File.create (FilePath "/test/file1.ext") (FileName "file1")
            File.create (FilePath "/test/file2.ext") (FileName "file2")
        |]
        Launchers = Array.empty
    }

[<Fact>]
let ``should not load a folder if no result`` () =
    let folder =
        let loadFiles _ _ = Array.empty

        App.loadFolder loadFiles {
            Path = FolderPath ""
            Pattern = Pattern.init "" false
            Launchers = Array.empty
        }

    folder =! None

[<Fact>]
let ``refresh should synchronize files`` () =
    let newFolder =
        let loadFiles _ _ = [|
            FilePath "file1", FileName ""
            FilePath "file3", FileName ""
        |]

        let save = id

        {
            Path = FolderPath ""
            Pattern = Pattern.init "" false
            Files = [|
                File.create (FilePath "file1") (FileName "")
                File.create (FilePath "file2") (FileName "")
            |]
            Launchers = Array.empty
        }
        |> App.refresh loadFiles save
        |> Option.get

    newFolder.Files.[0].Path.value =! "file1"
    newFolder.Files.[1].Path.value =! "file3"

[<Fact>]
let ``refresh should keep triggers`` () =
    let newFolder =
        let loadFiles _ _ = [|
            FilePath "file1", FileName ""
            FilePath "file2", FileName ""
        |]

        let save = id

        {
            Path = FolderPath ""
            Pattern = Pattern.init "" false
            Files = [|
                File.create (FilePath "file1") (FileName "") |> File.triggered
                File.create (FilePath "file2") (FileName "")
            |]
            Launchers = Array.empty
        }
        |> App.refresh loadFiles save
        |> Option.get

    newFolder.Files.[0].Triggered =! 1
    newFolder.Files.[1].Triggered =! 0
