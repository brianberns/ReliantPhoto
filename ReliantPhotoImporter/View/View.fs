namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout

module View =

    let view model dispatch =
        Window.create [
            Window.child (
                StackPanel.create [
                    StackPanel.children [
                        StackPanel.create [
                            StackPanel.orientation Orientation.Horizontal
                            StackPanel.children [
                                TextBlock.create [
                                    TextBlock.text "Import images to:"
                                ]
                                TextBlock.create [
                                    TextBlock.text "Folder"
                                ]
                                Button.create [
                                    Button.content "Browse"
                                ]
                            ]
                        ]
                    ]
                ]
            )
        ]
