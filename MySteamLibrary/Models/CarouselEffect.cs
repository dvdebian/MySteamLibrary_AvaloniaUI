namespace MySteamLibrary.Models;

/// <summary>
/// Defines the available visual styles for the carousel display.
/// </summary>
public enum CarouselEffect
{
    ModernStack,      // Stacked cards with slight skew
    InvertedV,        // Classic perspective lean
    ModernStackArc,   // Cards arranged in a dipping curved arc
    FlatZoom,         // Standard zoom without perspective changes
    ConsoleShelf,     // Flat layout with tight horizontal overlap
    DeepSpiral,       // High scale reduction for 3D depth
    Wave,             // Vertical oscillation across the list
    CardsOnTable,     // Perspective making cards look like they lie flat
    Skyline           // Alternating vertical offsets
}