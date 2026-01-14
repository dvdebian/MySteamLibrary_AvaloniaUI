namespace MySteamLibrary.Models;

/// <summary>
/// Defines the available visual styles for the carousel display.
/// </summary>
public enum CarouselEffect
{
    // ===== ORIGINAL 9 EFFECTS =====
    ModernStack,      // Stacked cards with slight skew
    InvertedV,        // Classic perspective lean
    ModernStackArc,   // Cards arranged in a dipping curved arc
    FlatZoom,         // Standard zoom without perspective changes
    ConsoleShelf,     // Flat layout with tight horizontal overlap
    DeepSpiral,       // High scale reduction for 3D depth
    Wave,             // Vertical oscillation across the list
    CardsOnTable,     // Perspective making cards look like they lie flat
    Skyline,          // Alternating vertical offsets

    // ===== NEW 11 EFFECTS =====
    Tornado,          // Spiral rotation with vertical displacement
    Waterfall,        // Cascading diagonal flow
    Rollercoaster,    // Up and down arc motion
    FanSpread,        // Cards spread like a hand of cards
    Accordion,        // Compressed horizontal squeeze
    Pendulum,         // Swinging motion side to side
    Staircase,        // Diagonal ascending/descending steps
    Helix,            // 3D DNA-like double spiral
    Ripple,           // Concentric circular wave from center
    Bounce,           // Alternating bounce heights
    Cylinder,         // Wrapped around imaginary cylinder
    Perspective3D,    // Aggressive 3D perspective tilt
    Zipper,           // Interlocking zigzag pattern
    Domino,           // Progressive falling domino effect
    Rainbow,          // Smooth parabolic arc
    Telescope,        // Expanding scale from center
    Flip,             // Cards flipping/rotating 
    Orbit,            // Circular orbital motion
    Pyramid,          // Triangular stacking formation
    Drift             // Floating drift with random-like offsets
}