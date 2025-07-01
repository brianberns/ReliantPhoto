namespace Reliant.Photo

open System
open System.IO
open System.Threading

open Elmish

/// Messages that can change the directory model.
type DirectoryMessage =

    /// Load the current directory, if possible.
    | LoadDirectory

    /// Some images in the current directory were loaded.
    | ImagesLoaded of SessionId * FileImageResult[]

    /// Loading of images in the current directory has finished.
    | DirectoryLoaded

    /// User has selected a directory to load.
    | DirectorySelected of DirectoryInfo

module DirectoryMessage =

    /// Browses to the given directory.
    let init dir =
        DirectoryModel.init dir,
        Cmd.ofMsg LoadDirectory

    /// Loads images in parallel within each chunk.
    let private loadChunks
        sessionId (token : CancellationToken) chunks : Effect<_> =
        fun dispatch ->
            let work =
                async {
                    for chunk in chunks do
                        if not token.IsCancellationRequested then
                            let! pairs = Async.Parallel chunk
                            dispatch (
                                ImagesLoaded (sessionId, pairs))
                }
            Async.Start(work, token)

    /// Height of each image.
    let private imageHeight = 150

    /// Loads images in a directory asynchronously.
    let private loadDirectory model : Subscribe<_> =
        fun dispatch ->

                // create async image chunks
            let chunks =
                model.Directory
                    |> ImageFile.tryLoadDirectory imageHeight
                    |> Seq.chunkBySize 50

                // load chunks
            let cts = new CancellationTokenSource()
            loadChunks
                model.SessionId cts.Token chunks dispatch

                // allow cancellation
            {
                new IDisposable with
                    member _.Dispose() =
                        cts.Cancel(); cts.Dispose()
            }

    /// Responds to the creation of a new file.
    let private onFileCreated
        sessionId token dispatch (args : FileSystemEventArgs) =
        async {
            let file = FileInfo(args.FullPath)
            do! FileInfo.waitForFileRead token file
            let! result =
                ImageFile.tryLoadImage
                    (Some imageHeight) file
            let pair = file, result
            dispatch (ImagesLoaded (sessionId, [|pair|]))
        } |> Async.Start

    /// Watches the model's directory for changes.
    let private watch model : Subscribe<_> =
        fun dispatch ->

                // watch for changes
            let cts = new CancellationTokenSource()
            let watcher =
                new FileSystemWatcher(
                    model.Directory.FullName,
                    EnableRaisingEvents = true)
            watcher.Created.Add(
                onFileCreated
                    model.SessionId
                    cts.Token
                    dispatch)

                // cleanup
            {
                new IDisposable with
                    member _.Dispose() =
                        watcher.Dispose()
                        cts.Cancel(); cts.Dispose()
            }

    /// Subscribes to loading images.
    let subscribe (model : DirectoryModel) : Sub<_> =
        let key = string model.SessionId
        [
            if model.IsLoading then
                [ key; "load" ], loadDirectory model

            [ key; "watch" ], watch model
        ]

    /// Updates the given model based on the given message.
    let update message (model : DirectoryModel) =
        match message with

            | LoadDirectory ->
                { model with IsLoading = true },   // trigger subscription
                Cmd.none

            | ImagesLoaded (sessionId, pairs) ->
                let model =
                    if sessionId = model.SessionId then
                        { model with
                            FileImageResults =
                                Array.append
                                    model.FileImageResults
                                    pairs }
                        else model   // ignore stale message
                model, Cmd.none

            | DirectoryLoaded ->
                { model with IsLoading = false },
                Cmd.none

            | DirectorySelected dir ->
                init dir
