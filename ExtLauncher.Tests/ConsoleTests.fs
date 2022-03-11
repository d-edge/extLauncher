module ExtLauncher.ConsoleTests

open System
open System.Collections.Generic
open Swensen.Unquote
open Xunit

let Terminal
    (strReader: Queue<string>)
    (keyReader: Queue<Console.TerminalKey>)
    (buffer: ResizeArray<string>)
    =
    let mutable left = 0
    let mutable top = 0
    { new Console.ITerminal with
        member _.ShowCursor() = ()
        member _.HideCursor() = ()
        member _.ToggleCursorVisibility() = ()
        member _.GetCursorPosition() =
            left, top
        member _.SetCursorPosition(l, t) =
            left <- l
            top <- t
        member _.ReadKey () =
            keyReader.Dequeue()
        member _.ReadLine() =
            strReader.Dequeue()
        member _.Write str =
            if top < buffer.Count then
                buffer[top] <- buffer[top].Insert(left, str)
            else
                buffer.Add str
                left <- str.Length
        member _.WriteLine str =
            if top < buffer.Count then
                buffer[top] <- str
            else
                buffer.Add str
            top <- top + 1
            left <- 0
        member this.Markup str = this.Write str
        member this.MarkupLine str = this.WriteLine str
        member _.ClearLine() =
            buffer[top] <- String.Empty
            left <- 0
    }

let newTerminal = Terminal (Queue())
let noConsoleModifier = enum<ConsoleModifiers> 0
let enterKey = ConsoleKey.Enter, char ConsoleKey.Enter, noConsoleModifier
let downKey = ConsoleKey.DownArrow, char ConsoleKey.DownArrow, noConsoleModifier
let upKey = ConsoleKey.UpArrow, char ConsoleKey.UpArrow, noConsoleModifier
let backspaceKey = ConsoleKey.Backspace, char ConsoleKey.Backspace, noConsoleModifier
let escapeKey = ConsoleKey.Escape, char ConsoleKey.Escape, ConsoleModifiers.Alt
let aKey k = enum<ConsoleKey>(Char.ToUpper k |> int), k, noConsoleModifier
let [<Literal>] PromptTitle = "Search and choose"
let [<Literal>] PrintedTitle = "[teal]" + PromptTitle + "[/] "

let printedLines maxChoices itemsCount chosenNum = [
    PrintedTitle
    for n in 1..itemsCount do
        $"""[yellow]{if n = chosenNum then ">" else " "} [/]{n}"""
    for _ in itemsCount+1..maxChoices do
        ""
    ]

[<Fact>]
let ``prompt should print choices`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 5

    List.ofSeq lines =! printedLines 5 3 1

[<Fact>]
let ``prompt should go down`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ downKey; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 5

    List.ofSeq lines =! printedLines 5 3 2

[<Fact>]
let ``prompt should go up`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ downKey; upKey; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 5

    List.ofSeq lines =! printedLines 5 3 1

[<Fact>]
let ``prompt should stay up`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ upKey; upKey; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 5

    List.ofSeq lines =! printedLines 5 3 1

[<Fact>]
let ``prompt should stay down`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ downKey; downKey; downKey; downKey; downKey; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 5

    List.ofSeq lines =! printedLines 5 3 3

[<Fact>]
let ``prompt should choose the second choice and clear`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ downKey; enterKey ]
    let term = newTerminal keyReader lines

    let chosen =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 5

    chosen =! Some 2
    Seq.forall ((=) "") lines =! true

[<Fact>]
let ``prompt should print error if no match`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> Array.empty
        |> Console.prompt term PromptTitle 1

    List.ofSeq lines =! [
        PrintedTitle
        "No items match your search."
    ]

[<Fact>]
let ``prompt should print the search title`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ aKey 't'; aKey 'e'; aKey 's'; aKey 't'; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 1

    Seq.head lines =! $"{PrintedTitle}test"

[<Fact>]
let ``prompt should print the search chars supporting backspace`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ aKey 't'; aKey 'e'; backspaceKey; aKey 's'; aKey 't'; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 1

    Seq.head lines =! $"{PrintedTitle}tst"

[<Fact>]
let ``prompt should clear when exit`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ ConsoleKey.Escape, char ConsoleKey.Escape, noConsoleModifier ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term PromptTitle 5

    Seq.forall ((=) "") lines =! true
