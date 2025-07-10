namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL

module View =

    /// Creates a view of the given model.
    let view systemScale model dispatch =
        Grid.create [
            Grid.children [

                    // directory view
                Border.create [
                    Control.isVisible
                        model.ImageModelOpt.IsNone
                    Border.child (
                        DirectoryView.view
                            model.DirectoryModel dispatch
                    )
                ]
                    // image view
                match model.ImageModelOpt with
                    | None -> ()
                    | Some imgModel ->
                        ImageView.view
                            systemScale imgModel dispatch
            ]
        ]
