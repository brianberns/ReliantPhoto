namespace Reliant.Photo

open Avalonia
open Avalonia.Input
open Avalonia.Media.Imaging

module Cursor =

    /// Wait cursor.
    let wait = new Cursor(StandardCursorType.Wait)

    /// Hand cursor.
    let hand = new Cursor(StandardCursorType.Hand)