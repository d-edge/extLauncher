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
        member _.ClearLine() =
            buffer[top] <- String.Empty
            left <- 0
    }

let newTerminal = Terminal (Queue())
let enterKey = ConsoleKey.Enter, char ConsoleKey.Enter, enum<ConsoleModifiers> 0
let downKey = ConsoleKey.DownArrow, char ConsoleKey.DownArrow, enum<ConsoleModifiers> 0
let upKey = ConsoleKey.UpArrow, char ConsoleKey.UpArrow, enum<ConsoleModifiers> 0
let backspaceKey = ConsoleKey.Backspace, char ConsoleKey.Backspace, enum<ConsoleModifiers> 0
let escapeKey = ConsoleKey.Escape, char ConsoleKey.Escape, ConsoleModifiers.Alt
let aKey k = enum<ConsoleKey>(Char.ToUpper k |> int), k, enum<ConsoleModifiers> 0
let [<Literal>] SearchSentence = "[teal]Search a file to launch:[/] "

let printedLines maxChoices itemsCount chosenNum = [
    SearchSentence
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
        |> Console.prompt term 5

    List.ofSeq lines =! printedLines 5 3 1

[<Fact>]
let ``prompt should go down`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ downKey; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term 5

    List.ofSeq lines =! printedLines 5 3 2

[<Fact>]
let ``prompt should go up`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ downKey; upKey; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term 5

    List.ofSeq lines =! printedLines 5 3 1

[<Fact>]
let ``prompt should stay up`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ upKey; upKey; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term 5

    List.ofSeq lines =! printedLines 5 3 1

[<Fact>]
let ``prompt should stay down`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ downKey; downKey; downKey; downKey; downKey; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term 5

    List.ofSeq lines =! printedLines 5 3 3

[<Fact>]
let ``prompt should choose the second choice and clear`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ downKey; enterKey ]
    let term = newTerminal keyReader lines

    let chosen =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term 5

    chosen =! Some 2
    List.ofSeq lines =! [
        """Launching "2"..."""
        ""; ""; ""; ""; ""
    ]

[<Fact>]
let ``prompt should print error if no match`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> Array.empty
        |> Console.prompt term 1

    List.ofSeq lines =! [
        SearchSentence
        "No items match your search."
    ]

[<Fact>]
let ``prompt should print the search chars`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ aKey 't'; aKey 'e'; aKey 's'; aKey 't'; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term 1

    Seq.head lines =! $"{SearchSentence}test"

[<Fact>]
let ``prompt should print the search chars supporting backspace`` () =
    let lines = ResizeArray()
    let keyReader = Queue [ aKey 't'; aKey 'e'; backspaceKey; aKey 's'; aKey 't'; escapeKey ]
    let term = newTerminal keyReader lines

    let _ =
        fun _ -> [| 1; 2; 3 |]
        |> Console.prompt term 1

    Seq.head lines =! $"{SearchSentence}tst"
