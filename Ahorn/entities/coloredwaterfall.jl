module PandorasBoxColoredWaterfall

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/coloredWaterfall" ColoredWaterfall(x::Integer, y::Integer, color="LightSkyBlue")

segmentCache = Dict{String, Any}()

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const waterSegmentMatrix = [
    1 0 0 0 0 0 0 1 1 1;
    1 0 0 0 0 0 0 1 1 1;
    1 0 0 0 0 0 0 1 1 1;
    1 0 0 0 0 0 0 1 1 1;
    1 0 0 0 0 0 0 1 1 1;
    1 0 0 0 0 0 0 1 1 1;
    1 0 0 0 0 0 0 1 1 1;
    1 0 0 0 0 0 0 1 1 1;
    1 1 0 0 0 0 0 0 1 1;
    1 1 0 0 0 0 0 0 1 1;
    1 1 1 0 0 0 0 0 0 1;
    1 1 1 0 0 0 0 0 0 1;
    1 1 1 0 0 0 0 0 0 1;
    1 1 1 0 0 0 0 0 0 1;
    1 1 1 0 0 0 0 0 0 1;
    1 1 1 0 0 0 0 0 0 1;
    1 1 1 0 0 0 0 0 0 1;
    1 1 1 0 0 0 0 0 0 1;
    1 1 0 0 0 0 0 0 1 1;
    1 1 0 0 0 0 0 0 1 1;
    1 1 0 0 0 0 0 0 1 1;
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

function getSegment(color)
    if haskey(segmentCache, color)
        return segmentCache[color]

    else
        baseColor = getColor(color)
        fillColor = baseColor .* 0.3
        surfaceColor = baseColor .* 0.8

        waterSegment = Ahorn.matrixToSurface(
            waterSegmentMatrix,
            [
                fillColor,
                surfaceColor
            ]
        )

        segmentCache[color] = waterSegment

        return waterSegment
    end
end

function getHeight(entity::ColoredWaterfall, room::Maple.Room)
    waterEntities = filter(e -> e.name == "water", room.entities)
    waterRects = Ahorn.Rectangle[
        Ahorn.Rectangle(
            Int(get(e.data, "x", 0)),
            Int(get(e.data, "y", 0)),
            Int(get(e.data, "width", 8)),
            Int(get(e.data, "height", 8))
        ) for e in waterEntities
    ]

    width, height = room.size
    x, y = Int(get(entity.data, "x", 0)), Int(get(entity.data, "y", 0))
    tx, ty = floor(Int, x / 8) + 1, floor(Int, y / 8) + 1

    wantedHeight = 8 - y % 8
    while wantedHeight < height - y
        rect = Ahorn.Rectangle(x, y + wantedHeight, 8, 8)

        if any(Ahorn.checkCollision.(waterRects, Ref(rect)))
            break
        end

        if get(room.fgTiles.data, (ty + 1, tx), '0') != '0'
            break
        end

        wantedHeight += 8
        ty += 1
    end

    return wantedHeight
end

const placements = Ahorn.PlacementDict(
    "Waterfall (Pandora's Box)" => Ahorn.EntityPlacement(
        ColoredWaterfall
    )
)

Ahorn.editingOptions(entity::ColoredWaterfall) = Dict{String, Any}(
    "color" => colors
)

function Ahorn.selection(entity::ColoredWaterfall, room::Maple.Room)
    x, y = Ahorn.position(entity)
    height = getHeight(entity, room)

    return Ahorn.Rectangle(x, y, size(waterSegmentMatrix, 2), height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ColoredWaterfall, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))
    color = get(entity.data, "color", "LightSkyBlue")

    height = getHeight(entity, room)
    segment = getSegment(color)
    segmentHeight, segmentWidth = size(waterSegmentMatrix)

    Ahorn.Cairo.save(ctx)

    Ahorn.rectangle(ctx, 0, 0, segmentWidth, height)
    Ahorn.clip(ctx)

    for i in 0:segmentHeight:ceil(Int, height / segmentHeight) * segmentHeight
        Ahorn.drawImage(ctx, segment, 0, i)
    end
    
    Ahorn.restore(ctx)
end

end