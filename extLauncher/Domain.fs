namespace extLauncher

open System

type FileName =
    | FileName of string

    member this.value =
        match this with
        | FileName v -> v

type FolderPath =
    | FolderPath of string

    member this.value =
        match this with
        | FolderPath v -> v

type FilePath =
    | FilePath of string

    member this.value =
        match this with
        | FilePath v -> v
        
    member this.folder =
        match this with
        | FilePath v -> v |> System.IO.Path.GetDirectoryName |> FolderPath

[<CustomEquality; CustomComparison>]
type File = {
    Path: FilePath
    Name: FileName
    Triggered: int32
} with

    override this.GetHashCode() = this.Path.GetHashCode()

    override this.Equals other =
        match other with
        | :? File as other -> this.Path.value = other.Path.value
        | _ -> ArgumentException() |> raise

    interface IComparable with
        member this.CompareTo other =
            match other with
            | :? File as other ->
                if this.Triggered = other.Triggered then
                    String.Compare(this.Name.value, other.Name.value, true)
                elif this.Triggered > other.Triggered then
                    -1
                else
                    1
            | _ -> ArgumentException() |> raise

module File =

    let create (FilePath id) (FileName name) = {
        Path = FilePath id
        Name = FileName name
        Triggered = 0
    }

    let triggered file = {
        file with
            Triggered = file.Triggered + 1
    }
    
type Choose =
    | File = 0
    | Directory = 1

module Choose =
    let init =
        function
        | 0 -> Choose.File
        | 1 -> Choose.Directory
        | _ -> failwith "Invalid value"

type Launcher = {
    Name: string
    Path: FilePath
    Arguments: string
    Choose: Choose
} with

    static member name this = this.Name

module Launcher =
    let buildArgs launcher tolaunch =
        if String.IsNullOrEmpty launcher.Arguments then
            tolaunch
        else
            launcher.Arguments.Replace("%s", $"\"%s{tolaunch}\"")

type Pattern =
    | WildcardPattern of string
    | RegexPattern of string

    member this.value =
        match this with
        | WildcardPattern v
        | RegexPattern v -> v

    member this.isRegex =
        match this with
        | WildcardPattern _ -> false
        | RegexPattern _ -> true

    static member from value isRegex =
        if isRegex then
            RegexPattern value
        else
            WildcardPattern value

module Pattern =
    let value =
        function
        | WildcardPattern p
        | RegexPattern p -> p

    let isRegex =
        function
        | WildcardPattern _ -> false
        | RegexPattern _ -> true

    let init value isRegex =
        if isRegex then
            RegexPattern value
        else
            WildcardPattern value

type Folder = {
    Path: FolderPath
    Pattern: Pattern
    Files: File array
    Launchers: Launcher array
}

module Helpers =

    let inline searchByName (items: 't array) (getName: 't -> string) (str: string) : 't array =
        if String.IsNullOrEmpty str then
            items
        else
            items
            |> Array.filter (fun item ->
                match item |> getName |> Option.ofObj with
                | Some name -> name.Contains(str, StringComparison.OrdinalIgnoreCase)
                | None -> false
            )
