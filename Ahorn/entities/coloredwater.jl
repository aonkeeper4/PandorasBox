module PandorasBoxColoredWater

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/coloredWater" ColoredWater(x::Integer, y::Integer, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, hasTop::Bool=true, hasBottom::Bool=false, hasLeft::Bool=false, hasRight::Bool=false, color::String="LightSkyBlue")

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

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const placements = Ahorn.PlacementDict(
    "Water (Pandora's Box)" => Ahorn.EntityPlacement(
        ColoredWater,
        "rectangle"
    ),
)

Ahorn.editingOptions(entity::ColoredWater) = Dict{String, Any}(
    "color" => colors
)

Ahorn.minimumSize(entity::ColoredWater) = 8, 8
Ahorn.resizable(entity::ColoredWater) = true, true

Ahorn.selection(entity::ColoredWater) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ColoredWater, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    rawColor = get(entity.data, "color", "LightSkyBlue")
    color = getColor(rawColor)

    Ahorn.drawRectangle(ctx, 0, 0, width, height, color .* (1.0, 1.0, 1.0, 0.4), color)
end

end