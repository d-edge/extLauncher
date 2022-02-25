module ExtLauncher.Console

open System
open Spectre.Console

let showCursor () = Console.CursorVisible <- true
let hideCursor () = Console.CursorVisible <- false
let setCursorPosition left top = Console.SetCursorPosition(left, top)
let getCursorTop () = Console.CursorTop
let clearLine () = String(' ', Console.BufferWidth - 1) |> printf "%s"

let clearUp cursorTop count =
    for top = cursorTop to cursorTop + count do
        setCursorPosition 0 top
        clearLine ()
    setCursorPosition 0 cursorTop

let readKey () =
    let consoleKey = Console.ReadKey true
    (consoleKey.Key, consoleKey.KeyChar)

let printNoMatch () =
    printfn "No items match your search."

let checkNoMatch (search: string -> 'T array) =
    if search String.Empty |> Array.isEmpty then
        printNoMatch ()
        None
    else
        Some search

let prompt<'T> maxChoices (search: string -> 'T array) =

    for _ in 0..maxChoices do printfn "" // allocate buffer area
    let cursorTop = getCursorTop () - maxChoices - 1

    let search str =
        let choices = search str |> Array.truncate maxChoices
        (choices, str, 0)

    let print (choices: 'T array, str, pos) =
        hideCursor ()
        let pos = max 0 (min (Array.length choices - 1) pos)
        clearUp cursorTop maxChoices
        printfn ""
        if Array.isEmpty choices then
            printNoMatch ()
        else
            choices
            |> Array.iteri (fun i choice ->
                sprintf "[yellow]%s[/]%s"
                    (if i = pos then "> " else "  ")
                    (string choice)
                |> AnsiConsole.MarkupLine)
        setCursorPosition 0 cursorTop
        AnsiConsole.Markup $"[teal]Search a file to launch:[/] %s{str}"
        showCursor ()
        (choices, str, pos)

    let rec read (choices: 'T array, str, pos) =
        match readKey () with
        | ConsoleKey.Escape, _ ->
            clearUp cursorTop maxChoices
            None
        | ConsoleKey.Enter, _ ->
            if Array.isEmpty choices
            then read (choices, str, pos)
            else Some choices[pos]
        | ConsoleKey.UpArrow, _ ->
            print (choices, str, pos - 1) |> read
        | ConsoleKey.DownArrow, _ ->
            print (choices, str, pos + 1) |> read
        | ConsoleKey.Backspace, _ ->
            if str.Length = 0
            then read (choices, str, pos)
            else search str[..^1] |> print |> read
        | _, key ->
            search $"{str}{key}" |> print |> read

    search String.Empty |> print |> read
