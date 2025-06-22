namespace Reliant.Photo

open System
open System.Diagnostics
open System.IO
open System.Windows.Input

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Threading

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png

/// Underlying state.
type Model =
    {
        /// Directory containing images to browse.
        Directory : DirectoryInfo

        /// Current or upcoming image file, if any. This is set
        /// before the image itself is loaded.
        FileOpt : Option<FileInfo>

        /// Current loaded image, if any. This will be the old
        /// image when starting to browse to a new one.
        ImageOpt : Option<Bitmap>

        /// User can browse to previous image?
        HasPreviousImage : bool

        /// User can browse to next image?
        HasNextImage : bool
    }

/// Messages that can change the underlying state.
type Message =

    /// Load the current image file, if possible.
    | LoadImage

    /// The current image file was (maybe) loaded.
    | ImageLoaded of Option<Bitmap>

    /// Browse to previous image in directory, if possible.
    | PreviousImage

    /// Browse to next image in directory, if possible.
    | NextImage

module Image =

    /// Supported image file extentions.
    let supportedExtensions =
        set [
            ".bmp"
            ".gif"
            ".jpeg"
            ".jpg"
            ".pbm"
            ".png"
            ".tif"
            ".tiff"
            ".tga"
            ".qoi"
            ".webp"
        ]

    /// Browses from the current image to another image within
    /// the current directory.
    let private browseImage incr state =

            // get all candidate files for browsing
        let files =
            state.Directory.GetFiles()
                |> Array.where (fun file ->
                    supportedExtensions.Contains(
                        file.Extension.ToLower()))

            // find index of file we're browsing to, if possible
        let browseIdxOpt =
            option {
                let! curFile = state.FileOpt
                let! curIdx =
                    files
                        |> Array.tryFindIndex (fun file ->
                            file.FullName = curFile.FullName)
                let browseIdx = curIdx + incr
                if browseIdx >= 0 && browseIdx < files.Length then
                    return browseIdx
            }

            // update state accordingly
        match browseIdxOpt with
            | Some browseIdx ->
                { state with
                    FileOpt = Some files[browseIdx]
                    HasPreviousImage = browseIdx > 0
                    HasNextImage = browseIdx < files.Length - 1 }
            | None ->
                { state with
                    FileOpt = None
                    HasPreviousImage = false
                    HasNextImage = false }

    /// Browses to the given image file.
    let init (file : FileInfo) =
        browseImage 0 {
            Directory = file.Directory
            FileOpt = Some file
            ImageOpt = None
            HasPreviousImage = false
            HasNextImage = false
        },
        Cmd.ofMsg LoadImage

    /// PNG encoder.
    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

    /// Tries to load a bitmap from the given image file.
    let tryLoadBitmap path =
        async {
            try
                    // load image to PNG format
                use stream = new MemoryStream()
                do
                    use image = Image.Load(path : string)
                    image.SaveAsPng(stream, pngEncoder)
                    stream.Position <- 0

                    // create bitmap on UI thread
                let! bitmap =
                    Dispatcher.UIThread.InvokeAsync(fun () ->
                        new Bitmap(stream))
                        .GetTask()
                        |> Async.AwaitTask
                return Some bitmap

            with exn ->
                Trace.WriteLine($"{path}: {exn.Message}")
                return None
        }

    /// Updates the given state based on the given message.
    let update message state =
        match message with

                // start browsing to an image
            | LoadImage ->
                match state.FileOpt with
                    | Some file ->
                        let cmd =
                            Cmd.OfAsync.perform
                                tryLoadBitmap
                                file.FullName
                                ImageLoaded
                        state, cmd
                    | None -> failwith "No file to load"

                // finish browsing to an image
            | ImageLoaded bitmapOpt ->
                { state with ImageOpt = bitmapOpt },
                Cmd.none

                // browse to previous image
            | PreviousImage  ->
                browseImage -1 state,
                Cmd.ofMsg LoadImage

                // browse to next image
            | NextImage  ->
                browseImage 1 state,
                Cmd.ofMsg LoadImage

    /// Button height and width.
    let private browseButtonSize = 50

    /// Creates a browse button.
    let private createBrowseButton text callback =
        Button.create [
            Button.content (
                Viewbox.create [
                    Viewbox.stretch Stretch.Uniform
                    Viewbox.stretchDirection StretchDirection.Both
                    Viewbox.child (
                        TextBlock.create [
                            TextBlock.text text
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.textWrapping TextWrapping.NoWrap
                        ]
                    )
                ]
            )
            Button.height browseButtonSize
            Button.horizontalAlignment HorizontalAlignment.Stretch
            Button.verticalAlignment VerticalAlignment.Stretch
            Button.horizontalContentAlignment HorizontalAlignment.Center
            Button.verticalContentAlignment VerticalAlignment.Center
            Button.onClick callback
        ]

    /// Creates a browse panel, with or without a button.
    let private createBrowsePanel dock text hasButton callback =
        DockPanel.create [
            DockPanel.width browseButtonSize
            DockPanel.dock dock
            DockPanel.children [
                if hasButton then
                    createBrowseButton text callback
            ]
        ]

    /// Creates a panel that can display images.
    let private createImagePanel state dispatch =
        DockPanel.create [
            DockPanel.children [

                createBrowsePanel
                    Dock.Left "◀"
                    state.HasPreviousImage
                    (fun _ -> dispatch PreviousImage)

                createBrowsePanel
                    Dock.Right "▶"
                    state.HasNextImage
                    (fun _ -> dispatch NextImage)

                match state.ImageOpt with
                    | Some image ->
                        Image.create [
                            Image.source image
                        ]
                    | None ->
                        TextBlock.create []
            ]
        ]

    /// Creates an invisible border that handles key bindings.
    let private createInvisibleBorder state dispatch child =
        Border.create [

            Border.focusable true
            Border.background "Transparent"

            Border.keyBindings [
                if state.HasPreviousImage then
                    KeyBinding.create [
                        KeyBinding.key Key.Left
                        KeyBinding.execute (fun _ -> dispatch PreviousImage)
                    ]
                if state.HasNextImage then
                    KeyBinding.create [
                        KeyBinding.key Key.Right
                        KeyBinding.execute (fun _ -> dispatch NextImage)
                    ]
            ]

            Border.onLoaded (fun e ->
                let border = e.Source :?> Border   // grab focus
                border.Focus() |> ignore)

            Border.child (child : IView)
        ]

    /// Creates a view of the given state.
    let view state dispatch =
        createImagePanel state dispatch
            |> createInvisibleBorder state dispatch
