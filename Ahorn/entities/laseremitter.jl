module PandorasBoxLaserEmitter

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/laserEmitter" LaserEmitter(x::Integer, y::Integer, flag::String="", color::String="White", direction::String="Right", inverted::Bool=false, beamDuration::Int=-1)

const directions = String["Left", "Right", "Up"]
const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const placements = Ahorn.PlacementDict(
    "Laser Emitter ($direction) (Pandora's Box)" => Ahorn.EntityPlacement(
        LaserEmitter,
        "point",
        Dict{String, Any}(
            "direction" => direction
        )
    ) for direction in directions
)

Ahorn.editingOptions(entity::LaserEmitter) = Dict{String, Any}(
    "direction" => directions,
    "color" => colors
)

sprite = "objects/pandorasBox/laser/emitter/idle0"

function Ahorn.selection(entity::LaserEmitter)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserEmitter, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1.0)

end