namespace Reliant.Photo

open Avalonia
open Avalonia.Input
open Avalonia.Media.Imaging

module Cursor =

    /// Creates a new cursor from a resource.
    let private create name =
        use stream = Resource.get name
        use bitmap = new Bitmap(stream)
        new Cursor(bitmap, PixelPoint(0, 0))

    /// Wait cursor.
    let wait = new Cursor(StandardCursorType.Wait)

    /// Open hand cursor.
    let openHand = create "View.palm.png"

    /// Closed hand cursor.
    let closedHand = create "View.hold.png"
