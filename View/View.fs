namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL

module View =

    /// Creates a view of the given model.
    let view dpiScale model dispatch =
        Grid.create [
            Grid.children [

                    // directory view
                Border.create [
                    Border.isVisible (model.Mode = Mode.Directory)
                    Border.child (
                        DirectoryView.view
                            model.DirectoryModel dispatch
                    )
                ]
                    // image view
                Border.create [
                    Border.isVisible (model.Mode = Mode.Image)
                    Border.child (
                        ImageView.view
                            dpiScale model.ImageModel dispatch
                    )
                ]
            ]
        ]
