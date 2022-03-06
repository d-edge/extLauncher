module ExtLauncher.DomainTests

open System
open FsCheck.Xunit
open Swensen.Unquote

[<Property>]
let ``File with the same Id should be equal`` (file1: File) (file2: File) =
    let file1 = { file1 with Id = file2.Id }
    file1 =! file2

[<Property>]
let ``File with different Id should not be equal`` (file1: File) (file2: File) =
    let file1 = { file1 with Id = Guid.NewGuid().ToString() }
    let file2 = { file2 with Id = Guid.NewGuid().ToString() }
    file1 <>! file2

[<Property>]
let ``File with a higher trigger should precede in the sort order`` (file1: File) (file2: File) =
    let file1 = { file1 with Triggered = file2.Triggered + 1 }
    compare file1 file2 =! -1

[<Property>]
let ``File with a lower trigger should follow in the sort order`` (file1: File) (file2: File) =
    let file1 = { file1 with Triggered = file2.Triggered - 1 }
    compare file1 file2 =! 1

[<Property>]
let ``File with the same trigger should be sorted alphabetically`` (file1: File) (file2: File) =
    let file1 = { file1 with Triggered = 0; Name = "a" }
    let file2 = { file2 with Triggered = 0; Name = "b" }
    compare file1 file2 =! -1

[<Property>]
let ``searchByName should search for the containing string ignoring case`` (file: File) (files: File array) =
    let file = { file with Name = "Hello World" }
    let files = Array.insertAt 0 file files
    File.searchByName files "world" =! [| file |]
