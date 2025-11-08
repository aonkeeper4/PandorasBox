module PandorasBoxLamp

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/lamp" Lamp(x::Integer, y::Integer, inverted::Bool=false, flag::String="", baseColor::String="White", lightColor::String="White", lightMode::String="Smooth", lightStartRadius::Int=48, lightEndRadius::Int=64)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const placements = Ahorn.PlacementDict(
    "Lamp (Pandora's Box)" => Ahorn.EntityPlacement(
        Lamp,
        "point"
    )
)

const lightModes = String[
    "Smooth",
    "Instant"
]

Ahorn.editingOptions(entity::Lamp) = Dict{String, Any}(
    "baseColor" => colors,
    "lightColor" => colors,
    "lightMode" => lightModes
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

gemTexture = "objects/pandorasBox/lamp/idle0"
baseTexture = "objects/pandorasBox/lamp/base"

function Ahorn.selection(entity::Lamp)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(gemTexture, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Lamp, room::Maple.Room)
    colorRaw = get(entity, "baseColor", "White")
    color = getColor(colorRaw)

    Ahorn.drawSprite(ctx, baseTexture, 0, 0, tint=color)
    Ahorn.drawSprite(ctx, gemTexture, 0, 0)
end

end