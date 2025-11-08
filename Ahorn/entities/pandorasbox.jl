module PandorasBoxPandorasBox

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/pandorasBox" PandorasBox(x::Integer, y::Integer, completeChapter::Bool=false, dialog::String="")

const placements = Ahorn.PlacementDict(
    "Pandora's Box (Pandora's Box)" => Ahorn.EntityPlacement(
        PandorasBox
    )
)

sprite = "objects/pandorasBox/pandorasBox/box_idle0"

function Ahorn.selection(entity::PandorasBox)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PandorasBox, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1.0)

end