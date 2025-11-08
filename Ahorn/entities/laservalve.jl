module PandorasBoxLaserValve

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/laserValve" LaserValve(x::Integer, y::Integer, direction::String="Right", delay::Int=4)

const directionRotations = Dict{String, Number}(
    "Right" => 0,
    "Down" => pi / 2,
    "Left" => pi,
    "Up" => pi / 2 * 3
)

const placements = Ahorn.PlacementDict(
    "Laser Valve ($direction) (Pandora's Box)" => Ahorn.EntityPlacement(
        LaserValve,
        "point",
        Dict{String, Any}(
            "direction" => direction
        )
    ) for (direction, rot) in directionRotations
)

const offsets = Dict{String, Tuple{Int, Int}}(
    "Right" => (0, 0),
    "Down" => (16, 0),
    "Left" => (16, 16),
    "Up" => (0, 16)
)

Ahorn.editingOptions(entity::LaserValve) = Dict{String, Any}(
    "direction" => collect(keys(offsets))
)

sprite = "objects/pandorasBox/laser/valve/valve2"

function Ahorn.selection(entity::LaserValve)
    x, y = Ahorn.position(entity)

    direction = get(entity, "direction", "Right")
    rotation = get(directionRotations, direction, 0)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=0.5)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserValve, room::Maple.Room)
    direction = get(entity, "direction", "Right")
    rotation = get(directionRotations, direction, 0)

    Ahorn.drawSprite(ctx, sprite, offsets[direction]..., jx=0.5, jy=0.5, rot=rotation)
end

end