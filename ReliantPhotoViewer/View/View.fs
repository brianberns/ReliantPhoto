namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input

module View =

    /// Creates a mode-specific view.
    let private createModeView model dispatch =
        match model with
            | DirectoryMode (dirModel, _) ->
                DirectoryView.view dirModel dispatch
                    :> IView
            | ImageMode (_, imgModel) ->
                ImageView.view imgModel dispatch

    /// Creates key bindings.
    let private createKeyBindings model dispatch =

        /// Triggers the given message on the given key presses.
        let createBindings keys message =
            [
                for key in keys do
                    KeyBinding.create [
                        KeyBinding.key key
                        KeyBinding.execute (fun _ ->
                            message
                                |> MkImageMessage
                                |> dispatch)
                    ]
            ]

        /// Triggers a result message on the given key presses.
        let createResultBindings keys = function
            | Some ((file, result) : FileImageResult) ->
                result
                    |> ImageMessage.ofResult file
                    |> createBindings keys
            | None -> []

        [
            match model with
                | ImageMode (_, Situation_ situation) ->

                        // previous image
                    yield! createResultBindings
                        [ Key.Left; Key.PageUp ]
                        situation.PreviousResultOpt

                        // next image
                    yield! createResultBindings
                        [ Key.Right; Key.PageDown ]
                        situation.NextResultOpt

                        // delete file
                    yield! createBindings
                        [ Key.Delete ]
                        DeleteFile

                        // full-screen on
                    yield! createBindings
                        [ Key.F11 ]
                        (FullScreen true)

                        // full-screen off
                    yield! createBindings
                        [ Key.Escape ]
                        (FullScreen false)

                | _ -> ()
        ]

    /// Creates a view of the given model.
    let view model dispatch =

        Window.create [

                // directory vs. image mode
            Window.child (
                createModeView model dispatch)

                // key bindings
            Window.keyBindings (
                createKeyBindings model dispatch)
        ]
