module extLauncher.DomainTests

open System
open FsCheck.Xunit
open Swensen.Unquote

[<Property>]
let ``File with the same Path should be equal`` (file1: File) (file2: File) =
    let file1 = { file1 with Path = file2.Path }
    file1 =! file2

[<Property>]
let ``File with different Path should not be equal`` (file1: File) (file2: File) =
    let file1 = {
        file1 with
            Path = Guid.NewGuid().ToString() |> FilePath
    }

    let file2 = {
        file2 with
            Path = Guid.NewGuid().ToString() |> FilePath
    }

    file1 <>! file2

[<Property>]
let ``File with a higher trigger should precede in the sort order`` (file1: File) (file2: File) =
    let file1 = {
        file1 with
            Triggered = file2.Triggered + 1
    }

    compare file1 file2 =! -1

[<Property>]
let ``File with a lower trigger should follow in the sort order`` (file1: File) (file2: File) =
    let file1 = {
        file1 with
            Triggered = file2.Triggered - 1
    }

    compare file1 file2 =! 1

[<Property>]
let ``File with the same trigger should be sorted alphabetically`` (file1: File) (file2: File) =
    let file1 = {
        file1 with
            Triggered = 0
            Name = FileName "a"
    }

    let file2 = {
        file2 with
            Triggered = 0
            Name = FileName "b"
    }

    compare file1 file2 =! -1

[<Property>]
let ``searchByName should search for the containing string ignoring case`` (file: File) (files: File array) =
    let file = {
        file with
            Name = FileName "Hello World"
    }

    let files = Array.insertAt 0 file files
    Helpers.searchByName files (fun f -> f.Name.value) "world" =! [| file |]
