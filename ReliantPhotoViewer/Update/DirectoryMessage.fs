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

    /// A file in the current directory was deleted.
    | ImageDeleted of SessionId * FileInfo

    /// Loading of images in the current directory has finished.
    | DirectoryLoaded of SessionId

module DirectoryMessage =

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
                    if not token.IsCancellationRequested then
                        dispatch (DirectoryLoaded sessionId)
                }
            Async.Start(work, token)

    /// Height of each image.
    let private imageHeight = 150

    /// Loads images in a directory asynchronously.
    let private loadDirectory (model : DirectoryModel)
        : Subscribe<_> =
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
        let work =
            async {
                let file = FileInfo(args.FullPath)
                do! FileInfo.waitForFileRead token file
                let! result =
                    ImageFile.tryLoadThumbnail imageHeight file
                let pair = file, result
                dispatch (ImagesLoaded (sessionId, [|pair|]))
            }
        Async.Start(work, token)

    /// Responds to the deletion of a file.
    let private onFileDeleted
        sessionId dispatch (args : FileSystemEventArgs) =
        let file = FileInfo(args.FullPath)
        dispatch (ImageDeleted (sessionId, file))

    /// Watches the model's directory for changes.
    let private watch (model : DirectoryModel) : Subscribe<_> =
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
            watcher.Deleted.Add(
                onFileDeleted
                    model.SessionId
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

    /// Handles the start of directory loading.
    let private onLoadDirectory model =
        { model with IsLoading = true },   // trigger subscription
        Cmd.none

    /// Handles newly loaded images.
    let private onImagesLoaded
        sessionId fileImageResults model =
        let model =
            if sessionId = model.SessionId then
                { model with
                    FileImageResults =
                        Array.append
                            model.FileImageResults
                            fileImageResults }
                else model   // ignore stale message
        model, Cmd.none

    /// Handles a deleted image.
    let private onImageDeleted
        sessionId (file : FileInfo) model =
        let model =
            if sessionId = model.SessionId then
                { model with
                    FileImageResults =
                        model.FileImageResults
                            |> Seq.where (fun (file_, _) ->   // to-do: improve efficiency
                                not (FileInfo.same file_ file))
                            |> Seq.toArray }
            else model   // ignore stale message
        model, Cmd.none

    /// Handles the end of directory loading.
    let private onDirectoryLoaded sessionId model =
        let model =
            if sessionId = model.SessionId then
                { model with IsLoading = false }
            else model   // ignore stale message
        model, Cmd.none

    /// Updates the given model based on the given message.
    let update message (model : DirectoryModel) =
        match message with
            | LoadDirectory -> onLoadDirectory model
            | ImagesLoaded (sessionId, fileImageResults) ->
                onImagesLoaded
                    sessionId fileImageResults model
            | ImageDeleted (sessionId, file) ->
                onImageDeleted sessionId file model
            | DirectoryLoaded sessionId ->
                onDirectoryLoaded sessionId model
