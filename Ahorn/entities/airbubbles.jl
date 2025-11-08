module PandorasBoxAirBubbles

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/airBubbles" AirBubbles(x::Integer, y::Integer, oneUse::Bool=false)

const placements = Ahorn.PlacementDict(
    "Air Bubbles (Pandora's Box)" => Ahorn.EntityPlacement(
        AirBubbles
    )
)

sprite = "objects/pandorasBox/airBubbles/idle00"

function Ahorn.selection(entity::AirBubbles)
    x, y = Ahorn.position(entity)
    
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::AirBubbles, room::Maple.Room)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end