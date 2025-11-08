module PandorasBoxLaserSensor

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/laserSensor" LaserSensor(x::Integer, y::Integer, flag::String="", color::String="White", blockLight::Bool=true, mode::String="Continuous")

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))
const modes = String["Continuous", "HitOnce"]

const placements = Ahorn.PlacementDict(
    "Laser Sensor (Pandora's Box)" => Ahorn.EntityPlacement(
        LaserSensor,
        "point"
    )
)

Ahorn.editingOptions(entity::LaserSensor) = Dict{String, Any}(
    "color" => colors,
    "mode" => modes
)

function getColor(color)
    if haskey(Ahorn.XNAColors.colors, color)
        return Ahorn.XNAColors.colors[color]

    else
        try
            return ((Ahorn.argb32ToRGBATuple(parse(Int, replace(color, "#" => ""), base=16))[1:3] ./ 255)..., 1.0)

        catch

        end
    end

    return (1.0, 1.0, 1.0, 1.0)
end

orbTexture = "objects/pandorasBox/laser/sensor/orb"
metalRingTexture = "objects/pandorasBox/laser/sensor/metal_ring"
lightRingTexture = "objects/pandorasBox/laser/sensor/light_ring"

function Ahorn.selection(entity::LaserSensor)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(metalRingTexture, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserSensor, room::Maple.Room)
    colorRaw = entity.color
    color = getColor(colorRaw)

    Ahorn.drawSprite(ctx, metalRingTexture, 0, 0)
    Ahorn.drawSprite(ctx, lightRingTexture, 0, 0, tint=color)
    Ahorn.drawSprite(ctx, orbTexture, 0, 0)
end

end