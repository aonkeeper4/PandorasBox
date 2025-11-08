module PandorasBoxGate

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/gate" Gate(x::Integer, y::Integer, inverted::Bool=false, flag::String="", texture::String="objects/pandorasBox/gate/")

const placements = Ahorn.PlacementDict(
    "Gate (Pandora's Box)" => Ahorn.EntityPlacement(
        Gate
    )
)

function getSprite(gate::Gate)
    texture = get(gate, "texture", "objects/pandorasBox/gate/")

    return texture * "gate0"
end

function Ahorn.selection(gate::Gate)
    x, y = Ahorn.position(gate)
    sprite = getSprite(gate)

    customSprite = Ahorn.getSprite(sprite, "Gameplay")

    if customSprite.width == 0 || customSprite.height == 0
        return Ahorn.Rectangle(x - 4, y - 4, 8, 8)

    else
        return Ahorn.getSpriteRectangle(sprite, x - 4, y, jx=0.0, jy=0.0)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, gate::Gate, room::Maple.Room)
    sprite = getSprite(gate)

    Ahorn.drawSprite(ctx, sprite, -4, 0, jx=0.0, jy=0.0)
end

end