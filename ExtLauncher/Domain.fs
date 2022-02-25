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
        | :? File as other -> this.Id = other.Name
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

    let search files str =
        if String.IsNullOrWhiteSpace str then files else
        files
        |> Array.filter (fun file ->
            file.Name.Contains(str, StringComparison.OrdinalIgnoreCase))

[<CLIMutable>]
type Folder =
    { Id: string
      Pattern: string
      OpenWith: string array
      Files: File array }
    override this.ToString() = $"[*.{this.Pattern}] {this.Id}"
