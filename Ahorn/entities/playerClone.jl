module PandorasBoxPlayerClone

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/playerClone" PlayerClone(x::Integer, y::Integer, mode::String="Inventory", flag::String="")

const placements = Ahorn.PlacementDict(
    "Player Clone (Unholy, Pandora's Box)" => Ahorn.EntityPlacement(
        PlayerClone
    )
)

const modes = String[
    "Inventory",
    "Backpack",
    "NoBackpack",
    "MadelineAsBadeline"
]

Ahorn.editingOptions(entity::PlayerClone) = Dict{String, Any}(
    "mode" => modes
)

sprite = "characters/player/sitDown00.png"

function Ahorn.selection(entity::PlayerClone)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PlayerClone) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1.0)

end