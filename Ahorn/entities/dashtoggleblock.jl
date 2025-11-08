module PandorasBoxDashToggleBlock

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/dashToggleBlock" DashTogleBlock(x::Integer, y::Integer, index::String="0", divisor::Integer=2)

const placements = Ahorn.PlacementDict(
    "Dash Toggle Block (Pandora's Box)" => Ahorn.EntityPlacement(
        DashTogleBlock,
        "rectangle"
    ),
)

Ahorn.minimumSize(entity::DashTogleBlock) = 8, 8
Ahorn.resizable(entity::DashTogleBlock) = true, true

Ahorn.selection(entity::DashTogleBlock) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DashTogleBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.8, 0.3, 1.0, 0.4), (0.8, 0.3, 1.0, 1.0))
end

end