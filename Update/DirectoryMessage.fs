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
    | ImagesLoaded of DirectoryInfo * FileImageResult[]

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
        dir (token : CancellationToken) chunks : Effect<_> =
        fun dispatch ->
            let work =
                async {
                    for chunk in chunks do
                        if not token.IsCancellationRequested then
                            let! pairs = Async.Parallel chunk
                            dispatch (ImagesLoaded (dir, pairs))
                }
            Async.Start(work, token)

    /// Height of each image.
    let private imageHeight = 150

    /// Loads images in a directory asynchronously.
    let private loadDirectory dir : Subscribe<_> =
        fun dispatch ->

                // create async image chunks
            let chunks =
                dir
                    |> ImageFile.tryLoadDirectory imageHeight
                    |> Seq.chunkBySize 50

                // load chunks
            let cts = new CancellationTokenSource()
            loadChunks dir cts.Token chunks dispatch

                // allow cancellation
            {
                new IDisposable with
                    member _.Dispose() =
                        cts.Cancel(); cts.Dispose()
            }

    /// Responds to the creation of a new file.
    let private onFileCreated dir dispatch (args : FileSystemEventArgs) =
        async {
            let file = FileInfo(args.FullPath)
            do! FileInfo.waitForFileRead file
            let! result =
                ImageFile.tryLoadImage
                    (Some imageHeight) file
            let pair = file, result
            dispatch (ImagesLoaded (dir, [|pair|]))
        } |> Async.Start

    /// Watches the given directory for changes.
    let private watch (dir : DirectoryInfo) : Subscribe<_> =
        fun dispatch ->

                // watch for changes
            let watcher =
                new FileSystemWatcher(
                    dir.FullName,
                    EnableRaisingEvents = true)
            watcher.Created.Add(onFileCreated dir dispatch)

                // cleanup
            {
                new IDisposable with
                    member _.Dispose() =
                        watcher.Dispose()
            }

    /// Subscribes to loading images.
    let subscribe (model : DirectoryModel) : Sub<_> =
        [
            if model.IsLoading then
                [ model.Directory.FullName; "load" ],
                loadDirectory model.Directory

            [ model.Directory.FullName; "watch" ],
            watch model.Directory
        ]

    /// Updates the given model based on the given message.
    let update message (model : DirectoryModel) =
        match message with

            | LoadDirectory ->
                { model with IsLoading = true },   // trigger subscription
                Cmd.none

            | ImagesLoaded (dir, pairs) ->
                let model =
                    if dir.FullName = model.Directory.FullName then
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
                if dir.FullName = model.Directory.FullName then
                    model, Cmd.none   // reloading the current directory could create a stealth stale message
                else init dir
