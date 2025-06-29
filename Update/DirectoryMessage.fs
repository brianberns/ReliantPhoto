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
    let private createEffect
        (dir : DirectoryInfo)
        (token : CancellationToken)
        chunks : Effect<_> =
        fun dispatch ->
            let work =
                async {
                    for chunk in chunks do
                        if not token.IsCancellationRequested then
                            let! pairs = Async.Parallel chunk
                            dispatch (ImagesLoaded (dir, pairs))
                }
            Async.Start(work, token)

    /// Creates a subscription that loads images asynchronously.
    let private startSub dir chunks : Subscribe<_> =
        fun dispatch ->
            let cts = new CancellationTokenSource()
            createEffect dir cts.Token chunks dispatch
            {
                new IDisposable with
                    member _.Dispose() =
                        cts.Cancel()
                        cts.Dispose()
            }

    /// Subscribes to loading images.
    let subscribe (model : DirectoryModel) : Sub<_> =
        [
            if model.IsLoading then
                let start =
                    model.Directory
                        |> ImageFile.tryLoadDirectory 150
                        |> Seq.chunkBySize 50
                        |> startSub model.Directory
                [ model.Directory.FullName ], start
        ]

    /// Updates the given model based on the given message.
    let update setTitle message (model : DirectoryModel) =
        match message with

            | LoadDirectory ->
                setTitle model.Directory.FullName   // side-effect
                { model with IsLoading = true },
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
