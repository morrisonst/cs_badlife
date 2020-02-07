// Implements Conway's Game Of Life badly
// https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life on a torus

open System
open System.IO
open System.Linq
open System.Text

let fileArgIdentifier : string = "file:"
let torusArgIdentifier : string = "torus:"

//let isToroidalBoard : bool = true
let isLiveCell : char = '*'
let isDeadCell : char = '_'

//let filePath = "sample_input.txt";
//let filePath = "sample_toad.txt";
//let filePath = "sample_blinker.txt";
//let filePath = "sample_glider.txt";

// <summary>
// Validates the supplied filePath.
// </summary>
let validateFilePath(filePath : string) : unit =
    if(String.IsNullOrWhiteSpace(filePath)) then
        raise (System.ArgumentException("Argument cannot be null or empty.", "filePath"))

// <summary>
// Reads the game data from a given file path.
// </summary>
let readGameDataAsSequence(filePath : string) = seq {
    use input = new StreamReader(filePath)
    while(not input.EndOfStream) do
        yield input.ReadLine()
}

// <summary>
// Validates and reads the game data from a given file path.
// </summary>
let readGameData(filePath : string) : string array =
    try
        validateFilePath(filePath)
        readGameDataAsSequence(filePath).ToArray()
    with
    | :? System.ArgumentException as ex -> raise (System.Exception("Error whilst attempting to read file.", ex))
    | :? System.Exception as ex -> raise (System.Exception( String.Concat("Error whilst attempting to read file: ", filePath, "."), ex))   

// <summary>
// Validates the game data.
// </summary>
let validateGameData(data : string array) : unit =
    if(not (data.Any(fun s -> (s.Contains(isLiveCell.ToString()) || (s.Contains(isDeadCell.ToString())))))) then
        raise (System.ArgumentException("Game data is not in a recognized format.", "data"))    
        
// <summary>
// Gets the number of live neighbors in the 9 cell vicinity of a given cell.
// </summary>
let getLiveNeighbourCount (x : int, y : int, board : bool array array , isToroidalBoard: bool) : int = 
    let mutable liveNeighbors = 0
    let boardHeight = board.Length
    let boardWidth = board.[0].Length
    
    //Check the row immediately above, the row itself and the row immediately below..
    for yOffset in [-1 .. 1] do

        // If y + yOffset is off the board edge, and the board is not on a torus then continue, otherwise.
        if (not ((((y + yOffset) < 0) || ((y + yOffset) >= boardHeight)) && not isToroidalBoard)) then
            
            // Loop around the edges if y + yOffset is off the board.
            let virtualY = (y + yOffset + boardHeight) % boardHeight
            
            //Check the column immediately to the left, the column itself and the column immediately to the right..
            for xOffset in [-1 .. 1] do
                
                // If  x + xOffset is off the board edge, and the board is not on a torus then continue, otherwise.
                if (not (((x + xOffset < 0) || ((x + xOffset) >= boardWidth)) && not isToroidalBoard)) then
                    
                    // Loop around the edges if x + xOffset is off the board.
                    let virtualX = (x + xOffset + boardWidth) % boardWidth
                    
                    // Count the neighbor cell at (h,k) if it is alive.
                    if (board.[virtualX].[virtualY]) then 
                        liveNeighbors <- liveNeighbors + 1
    
    //Decrement by 1 if (x,y) is alive since we counted it as a neighbor.
    if (board.[x].[y]) then
        liveNeighbors <- liveNeighbors - 1
    
    liveNeighbors                    


// <summary>
// Initialises the board with the supplie data.
// </summary>
let initialiseBoard(data : string array) : bool array array =
    //Initialise the new board with rows
    let board : bool array array = Array.zeroCreate data.Length

    for row in [0 .. data.Length-1] do
        //Initialise the new set of columns
        board.[row] <- Array.zeroCreate data.[row].Length
        
        for cell in [0 .. data.[row].Length-1] do
            //Determines if the given cell at position (x,y) in the supplied data is 
            //live or dead and sets the new board with the given value.
            board.[row].[cell] <- data.[row].[cell] = isLiveCell

    board

// <summary>
// Renders the game board with the supplied data.
// </summary>
let renderBoard(generation : int, boardData: bool array array) : unit =
    let mutable columnCount = 0
    
    if(boardData.Any()) then
        columnCount <- boardData.[0].Length
    
    //Use string builder as it is more efficient when concatenating strings.
    let grid = new StringBuilder()
    
    for row in [0 .. boardData.Length-1] do
        for cellIdx in [0 .. columnCount-1] do
            
            if(boardData.[row].[cellIdx]) then
                grid.Append(isLiveCell) |> ignore
            else
                grid.Append(isDeadCell) |> ignore
    
        grid.AppendLine() |> ignore
    
    //Reset the command line cursor position as it is less expensive than doing a Console.Clear()
    Console.SetCursorPosition(0, 1)
        
    printfn "Generation: %i" generation

    Console.Write(grid.ToString())

// <summary>
// Evolves  the game state to the next generation.
// </summary>
let evolve (currentBoardState : bool array array, isToroidal: bool) : bool array array = 
    let rowCount = currentBoardState.Length    
    let newBoardState : bool array array = Array.zeroCreate rowCount
        
    if(currentBoardState.Any()) then
        let columnCount = currentBoardState.[0].Length

        for row in [0 .. rowCount-1] do
            newBoardState.[row] <- Array.zeroCreate columnCount

            for cell in [0 .. columnCount-1] do

                let liveNeighbors = getLiveNeighbourCount(row, cell, currentBoardState, isToroidal)
            
                //Apply the Game of Life Rules
                //Rules are:
                //1. Any live cell with two or three neighbors survives.
                //2. Any dead cell with three live neighbors becomes a live cell.
                //3. All other live cells die in the next generation. Similarly, all other dead cells stay dead.
                if(currentBoardState.[row].[cell]) then 
                    newBoardState.[row].[cell] <- (not((liveNeighbors < 2) || (liveNeighbors > 3)))
                else 
                    newBoardState.[row].[cell] <- (liveNeighbors = 3)
    
    newBoardState

// <summary>
// Validates the supplied command line arguments.
// </summary>
let validateArgs(args: string array) : bool = 
    (args.Any(fun s -> (s.StartsWith(fileArgIdentifier) || (s.StartsWith(torusArgIdentifier)))))

// <summary>
// Displays command line arguments help message to user.
// </summary>
let showHelpMessage() : unit = 
    printfn "Supported command arguments [file:<filename> torus:<true|false>]"

// <summary>
// Extracts the string value of the given command line argument identifier.
// </summary>
let extractArg(args: string array, identifier : string) : string =
    args.FirstOrDefault(fun s -> (s.StartsWith(identifier))).Split(':').[1]


[<EntryPoint>]
let main(args) =
    
    Console.Clear()
    if(validateArgs(args) = false) then
        showHelpMessage()
        -1
    else
        try
            let filePath = extractArg(args, "file").ToLowerInvariant()
            let isToroidalBoard = (extractArg(args, "torus").ToLowerInvariant() = bool.TrueString.ToLowerInvariant())

            let data = readGameData(filePath).ToArray()
            validateGameData(data)

            let mutable board = initialiseBoard(data)

            let mutable generation = 0

            printfn "Press Escape key to finish or any other key to Evolve."

            let uiLoop() =
            
                renderBoard(generation, board)
            
                generation <- generation + 1
            
                board <- evolve(board, isToroidalBoard)

                not (Console.ReadKey(true).Key = ConsoleKey.Escape)

            while uiLoop() do ignore None

            42 // return an integer exit code
        with
        | :? System.Exception as ex -> Console.WriteLine(String.Format("An error occurred: => {0}.", ex.Message)); -1
