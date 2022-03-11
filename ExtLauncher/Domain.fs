namespace ExtLauncher

open System

[<CustomEquality; CustomComparison>]
type File =
    { Id: string
      Name: string
      Triggered: int32 }

    override this.ToString() = this.Name

    override this.GetHashCode() = this.Id.GetHashCode()

    override this.Equals other =
        match other with
        | :? File as other -> this.Id = other.Id
        | _ -> ArgumentException() |> raise

    interface IComparable with
        member this.CompareTo other =
            match other with
            | :? File as other ->
                if this.Triggered = other.Triggered then String.Compare(this.Name, other.Name, true)
                elif this.Triggered > other.Triggered then -1
                else 1
            | _ ->
                ArgumentException() |> raise

module File =

    let create id name =
        { Id = id; Name = name; Triggered = 0 }

    let triggered file =
        { file with Triggered = file.Triggered + 1 }

type Choose =
    | File = 0
    | Directory = 1

type Launcher =
    { Name: string
      Path: string
      Arguments: string
      Choose: Choose }
    override this.ToString() = this.Name

module Launcher =
    let buildArgs launcher tolaunch =
        if String.IsNullOrEmpty launcher.Arguments
        then tolaunch
        else launcher.Arguments.Replace("%s", $"\"{tolaunch}\"")

// Should be serializable to BSON
[<CLIMutable>]
type Folder =
    { Id: string
      Pattern: string
      IsRegex: bool
      Launchers: Launcher array
      Files: File array }
    override this.ToString() = this.Id

type Pattern =
    | WildcardPattern of string
    | RegexPattern of string

module Pattern =
    let value = function WildcardPattern p | RegexPattern p -> p
    let isRegex = function WildcardPattern _ -> false | RegexPattern _ -> true
    let from value isRegex = if isRegex then RegexPattern value else WildcardPattern value

module Helpers =

    let inline searchByName items str =
        if String.IsNullOrEmpty str then
            items
        else
            items
            |> Array.filter (fun item ->
                match (^T: (member Name: string) item) with
                | null -> false
                | name -> name.Contains(str, StringComparison.OrdinalIgnoreCase))
