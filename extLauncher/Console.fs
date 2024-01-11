module extLauncher.Console

open System
open Spectre.Console

type TerminalKey = ConsoleKey * char * ConsoleModifiers

type ITerminal =
    abstract member ShowCursor: unit -> unit
    abstract member HideCursor: unit -> unit
    abstract member ToggleCursorVisibility: unit -> unit
    abstract member GetCursorPosition: unit -> int32 * int32
    abstract member SetCursorPosition: int32 * int32 -> unit
    abstract member ReadKey: unit -> TerminalKey
    abstract member ReadLine: unit -> string
    abstract member Write: string -> unit
    abstract member WriteLine: string -> unit
    abstract member Markup: string -> unit
    abstract member MarkupLine: string -> unit
    abstract member ClearLine: unit -> unit

let Terminal =
    { new ITerminal with
        member _.ShowCursor() = Console.CursorVisible <- true
        member _.HideCursor() = Console.CursorVisible <- false

        member _.ToggleCursorVisibility() =
            Console.CursorVisible <- not Console.CursorVisible

        member _.GetCursorPosition() = Console.CursorLeft, Console.CursorTop

        member _.SetCursorPosition(left, top) =
            Console.SetCursorPosition((if left < 0 then 0 else left), (if top < 0 then 0 else top))

        member _.ReadKey() =
            let k = Console.ReadKey()
            k.Key, k.KeyChar, k.Modifiers

        member _.ReadLine() = Console.ReadLine()
        member _.Write str = AnsiConsole.Write str
        member _.WriteLine str = AnsiConsole.WriteLine str
        member _.Markup str = AnsiConsole.Markup str
        member _.MarkupLine str = AnsiConsole.MarkupLine str

        member this.ClearLine() =
            String(' ', Console.BufferWidth - 1) |> this.WriteLine
    }

[<Literal>]
let NoMatch = "No items match your search."

let clearUp (term: ITerminal) cursorTop count =
    for top = cursorTop to cursorTop + count do
        term.SetCursorPosition(0, top)
        term.ClearLine()

    term.SetCursorPosition(0, cursorTop)

let checkNoMatch (term: ITerminal) (search: string -> 'T array) =
    if search String.Empty |> Array.isEmpty then
        term.MarkupLine NoMatch
        None
    else
        Some search

let prompt<'T> (term: ITerminal) title (displayChoice: 'T -> string) maxChoices (search: string -> 'T array) =

    for _ in 0..maxChoices do
        term.WriteLine "" // allocate buffer area

    let cursorTop =
        let _, top = term.GetCursorPosition()
        top - maxChoices - 1

    let search str =
        let choices = search str |> Array.truncate maxChoices
        (choices, str, 0)

    let print (choices: 'T array, str, pos) =
        term.HideCursor()
        let pos = max 0 (min (Array.length choices - 1) pos)
        clearUp term cursorTop maxChoices
        term.WriteLine ""

        if Array.isEmpty choices then
            term.MarkupLine NoMatch
        else
            choices
            |> Array.iteri (fun i choice ->
                sprintf "[yellow]%s[/]%s" (if i = pos then "> " else "  ") (displayChoice choice)
                |> term.MarkupLine
            )

        term.SetCursorPosition(0, cursorTop)
        term.Markup $"[teal]%s{title}[/] %s{str}"
        term.ShowCursor()
        (choices, str, pos)

    let rec read (choices: 'T array, str, pos) =
        match term.ReadKey() with
        | ConsoleKey.Escape, _, ConsoleModifiers.Alt -> None // no clear alternative
        | ConsoleKey.Escape, _, _ ->
            term.Write(string '\u200B') // hack to force the clear
            clearUp term cursorTop maxChoices
            None
        | ConsoleKey.Enter, _, _ ->
            if Array.isEmpty choices then
                read (choices, str, pos)
            else
                clearUp term cursorTop maxChoices
                Some choices[pos]
        | ConsoleKey.UpArrow, _, _ -> read ((choices, str, pos - 1) |> print)
        | ConsoleKey.DownArrow, _, _ -> read ((choices, str, pos + 1) |> print)
        | ConsoleKey.Backspace, _, _ ->
            if str.Length = 0 then
                read (choices, str, pos)
            else
                read (search str[..^1] |> print)
        | _, key, _ -> read (search $"%s{str}%c{key}" |> print)

    search String.Empty |> print |> read
