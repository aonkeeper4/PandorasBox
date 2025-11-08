module PandorasBoxTileGlitcher

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/tileGlitcher" TileGlitcher(x::Integer, y::Integer, allowAir::Bool=false, transformAir::Bool=false, target::String="Both", threshold::Number=0.1, rate::Number=0.05, flag::String="", customFgTiles::String="", customBgTiles::String="")

const placements = Ahorn.PlacementDict(
    "Tile Glitcher (Pandora's Box)" => Ahorn.EntityPlacement(
        TileGlitcher,
        "rectangle"
    ),
)

Ahorn.editingOptions(entity::TileGlitcher) = Dict{String, Any}(
    "target" => String[
        "None",
        "FG",
        "BG",
        "Both"
    ]
)

Ahorn.minimumSize(entity::TileGlitcher) = 8, 8
Ahorn.resizable(entity::TileGlitcher) = true, true

Ahorn.selection(entity::TileGlitcher) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TileGlitcher, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.5, 0.3, 1.0, 0.4), (0.5, 0.3, 1.0, 1.0))
end

end