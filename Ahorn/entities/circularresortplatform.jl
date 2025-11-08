module PandorasBoxCircularResortPlatform

using ..Ahorn, Maple

@pardef CircularResortPlatform(x1::Integer, y1::Integer, x2::Integer=x1 + 16, y2::Integer=y1, width::Integer=Maple.defaultBlockWidth, texture::String="default", clockwise::Bool=true, speed::Number=1500.0, particles::Bool=true, attachToSolid::Bool=true, renderRail::Bool=true, lineFillColor::String="160b12", lineEdgeColor::String="2a1923", rotationFix::Bool=true) = Entity("pandorasBox/circularResortPlatform", x=x1, y=y1, nodes=Tuple{Int, Int}[(x2, y2)], width=width, texture=texture, clockwise=clockwise, speed=speed, particles=particles, attachToSolid=attachToSolid, renderRail=renderRail, lineFillColor=lineFillColor, lineEdgeColor=lineEdgeColor, rotationFix=rotationFix)

const placements = Ahorn.PlacementDict(
    "Circular Platform (Pandora's Box)" => Ahorn.EntityPlacement(
        CircularResortPlatform,
        "rectangle",
        Dict{String, Any}(),
        function(entity::CircularResortPlatform)
            x, y = Int(entity.data["x"]), Int(entity.data["y"])
            width = Int(get(entity.data, "width", 8))
            entity.data["x"], entity.data["y"] = x + width, y
            entity.data["nodes"] = Tuple{Int, Int}[(x, y)]
        end
    )
)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

Ahorn.editingOptions(entity::CircularResortPlatform) = Dict{String, Any}(
    "texture" => Maple.wood_platform_textures,
    "lineEdgeColor" => colors,
    "lineFillColor" => colors
)

Ahorn.resizable(entity::CircularResortPlatform) = true, false
Ahorn.minimumSize(entity::CircularResortPlatform) = 8, 0
Ahorn.nodeLimits(entity::CircularResortPlatform) = 1, 1

function renderPlatform(ctx::Ahorn.Cairo.CairoContext, texture::String, x::Number, y::Number, width::Number)
    tilesWidth = div(width, 8)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, "objects/woodPlatform/$texture", x + 8 * (i - 1), y, 8, 0, 8, 8)
    end

    Ahorn.drawImage(ctx, "objects/woodPlatform/$texture", x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, "objects/woodPlatform/$texture", x + tilesWidth * 8 - 8, y, 24, 0, 8, 8)
    Ahorn.drawImage(ctx, "objects/woodPlatform/$texture", x + floor(Int, width / 2) - 4, y, 16, 0, 8, 8)
end

function Ahorn.selection(entity::CircularResortPlatform)
    nx, ny = Int.(entity.data["nodes"][1])
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 8))

    return [Ahorn.Rectangle(x, y, width, 8), Ahorn.Rectangle(nx - 8, ny - 8, 16, 16)]
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CircularResortPlatform, room::Maple.Room)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 8))

    texture = get(entity.data, "texture", "default")

    renderPlatform(ctx, texture, x, y, width)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::CircularResortPlatform, room::Maple.Room)
    clockwise = get(entity.data, "clockwise", false)
    dir = clockwise ? 1 : -1

    centerX, centerY = Int.(entity.data["nodes"][1])
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    radius = sqrt((centerX - x)^2 + (centerY - y)^2) + width / 2

    Ahorn.drawCircle(ctx, centerX, centerY, radius, Ahorn.colors.selection_selected_fc)
    Ahorn.drawArrow(ctx, centerX + radius, centerY, centerX + radius, centerY + 0.001 * dir, Ahorn.colors.selection_selected_fc, headLength=6)
    Ahorn.drawArrow(ctx, centerX - radius, centerY, centerX - radius, centerY + 0.001 * -dir, Ahorn.colors.selection_selected_fc, headLength=6)
end

end