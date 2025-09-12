namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input

module View =

    /// Triggers the given message on the given key presses.
    let private createBindings dispatch keys message =
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
    let private createResultBindings dispatch keys = function
        | Some ((file, result) : FileImageResult) ->
            result
                |> ImageMessage.ofResult file
                |> createBindings dispatch keys
        | None -> []

    /// Creates a view of the given model.
    let view model dispatch =

        Window.create [

                // mode-specific view
            Window.child (
                match model with
                    | DirectoryMode (dirModel, _) ->
                        DirectoryView.view dirModel dispatch
                            :> IView
                    | ImageMode (_, imgModel) ->
                        ImageView.view imgModel dispatch
            )

                // key bindings
            Window.keyBindings [
                match model with
                    | ImageMode (_, Situation_ situation) ->

                            // previous image
                        yield! createResultBindings dispatch
                            [ Key.Left; Key.PageUp ]
                            situation.PreviousResultOpt

                            // next image
                        yield! createResultBindings dispatch
                            [ Key.Right; Key.PageDown ]
                            situation.NextResultOpt

                            // delete file
                        yield! createBindings dispatch
                            [ Key.Delete ]
                            DeleteFile

                            // full-screen on
                        yield! createBindings dispatch
                            [ Key.F11 ]
                            (FullScreen true)

                            // full-screen off
                        yield! createBindings dispatch
                            [ Key.Escape ]
                            (FullScreen false)

                    | _ -> ()
            ]
        ]
