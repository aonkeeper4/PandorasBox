module PandorasBoxLaserMirror

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/laserMirror" LaserMirror(x::Integer, y::Integer, opening::String="LeftUp")

const openingScales = Dict{String, Tuple{Integer, Integer}}(
    "LeftUp" => (1, 1),
    "UpRight" => (-1, 1),
    "RightDown" => (-1, -1),
    "DownLeft" => (1, -1)
)

const placements = Ahorn.PlacementDict(
    "Static Laser Mirror ($opening) (Pandora's Box)" => Ahorn.EntityPlacement(
        LaserMirror,
        "point",
        Dict{String, Any}(
            "opening" => opening
        ) 
    ) for (opening, rot) in openingScales
)

Ahorn.editingOptions(entity::LaserMirror) = Dict{String, Any}(
    "opening" => collect(keys(openingScales))
)

sprite = "objects/pandorasBox/laser/mirror/mirror_static"

function Ahorn.selection(entity::LaserMirror)
    x, y = Ahorn.position(entity)

    opening = get(entity, "opening", "LeftUp")
    scaleX, scaleY = get(openingScales, opening, (1, 1))

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=0.5, sx=scaleX, sy=scaleY)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserMirror, room::Maple.Room)
    opening = get(entity, "opening", "LeftUp")
    scaleX, scaleY = get(openingScales, opening, (1, 1))

    Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=0.5, sx=scaleX, sy=scaleY)
end

end