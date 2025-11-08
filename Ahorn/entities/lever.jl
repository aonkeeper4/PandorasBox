module PandorasBoxLever

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/lever" Lever(x::Integer, y::Integer, active::Bool=false, flag::String="")

const placements = Ahorn.PlacementDict(
    "Lever (Pandora's Box)" => Ahorn.EntityPlacement(
        Lever
    )
)

sprite = "objects/pandorasBox/lever/lever0"

function getSprite(lever::Lever)
    return get(lever, "active", false) ? "objects/pandorasBox/lever/lever4" : "objects/pandorasBox/lever/lever0"
end

function Ahorn.selection(lever::Lever)
    x, y = Ahorn.position(lever)
    sprite = getSprite(lever)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, lever::Lever, room::Maple.Room) = Ahorn.drawSprite(ctx, getSprite(lever), 0, 0, jx=0.5, jy=1.0)

end