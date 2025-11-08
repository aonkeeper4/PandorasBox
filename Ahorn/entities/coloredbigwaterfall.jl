module PandorasBoxColoredBigWaterfall

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/coloredBigWaterfall" ColoredBigWaterfall(x::Integer, y::Integer, width::Integer=16, height::Integer=64, layer::String="FG", color::String="LightSkyBlue")

const placements = Ahorn.PlacementDict(
    "Big Waterfall (FG) (Pandora's Box)" => Ahorn.EntityPlacement(
        ColoredBigWaterfall,
        "rectangle",
        Dict{String, Any}(
            "layer" => "FG"
        )
    ),
    "Big Waterfall (BG) (Pandora's Box)" => Ahorn.EntityPlacement(
        ColoredBigWaterfall,
        "rectangle",
        Dict{String, Any}(
            "layer" => "BG"
        )
    )
)

const segmentCache = Dict{String, Any}()

const waterSegmentLeftMatrix = [
    1 1 1 0 1 0;
    1 1 1 0 1 0;
    1 1 1 0 1 0;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 0 1 0;
    1 1 1 0 1 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
]

const waterSegmentRightMatrix = [
    0 1 0 1 1 1;
    0 1 0 1 1 1;
    0 1 0 1 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 1 0 1 1 1;
    0 1 0 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
]

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

function getSegments(color)
    if haskey(segmentCache, color)
        return segmentCache[color]

    else
        baseColor = getColor(color)
        fillColor = baseColor .* 0.3
        surfaceColor = baseColor .* 0.8

        waterSegmentRight = Ahorn.matrixToSurface(
            waterSegmentRightMatrix,
            [
                fillColor,
                surfaceColor
            ]
        )

        waterSegmentLeft = Ahorn.matrixToSurface(
            waterSegmentLeftMatrix,
            [
                fillColor,
                surfaceColor
            ]
        )

        segmentCache[color] = (waterSegmentLeft, waterSegmentRight)

        return segmentCache[color]
    end
end

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

Ahorn.minimumSize(entity::ColoredBigWaterfall) = 8, 8
Ahorn.resizable(entity::ColoredBigWaterfall) = true, true

Ahorn.selection(entity::ColoredBigWaterfall) = Ahorn.getEntityRectangle(entity)

Ahorn.editingOptions(entity::ColoredBigWaterfall) = Dict{String, Any}(
    "layer" => String["FG", "BG"],
    "color" => colors
)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ColoredBigWaterfall, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 64))

    color = get(entity.data, "color", "LightSkyBlue")
    baseColor = getColor(color)
    fillColor = baseColor .* 0.3

    segmentHeightLeft, segmentWidthLeft = size(waterSegmentLeftMatrix)
    segmentHeightRight, segmentWidthRight = size(waterSegmentRightMatrix)

    Ahorn.Cairo.save(ctx)

    Ahorn.rectangle(ctx, 0, 0, width, height)
    Ahorn.clip(ctx)

    waterSegmentLeft, waterSegmentRight = getSegments(color)

    for i in 0:segmentHeightLeft:ceil(Int, height / segmentHeightLeft) * segmentHeightLeft
        Ahorn.drawImage(ctx, waterSegmentLeft, 0, i)
        Ahorn.drawImage(ctx, waterSegmentRight, width - segmentWidthRight, i)
    end

    # Drawing a rectangle normally doesn't guarantee that its the same color as above
    if height >= 0 && width >= segmentWidthLeft + segmentWidthRight
        fillRectangle = Ahorn.matrixToSurface(fill(0, (height, width - segmentWidthLeft - segmentWidthRight)), [fillColor])
        Ahorn.drawImage(ctx, fillRectangle, segmentWidthLeft, 0)
    end
    
    Ahorn.restore(ctx)
end

end