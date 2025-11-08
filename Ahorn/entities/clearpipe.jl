module PandorasBoxMarioClearPipe

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/clearPipe" ClearPipe(x::Integer, y::Integer, texture::String="glass", surfaceSound::Integer=-1, hasPipeSolids::Bool=true, transportSpeed::Number=175.0, pipeWidth::Int=32)

const placements = Ahorn.PlacementDict(
    "Clear Pipe (Pandora's Box)" => Ahorn.EntityPlacement(
        ClearPipe,
        "point",
        Dict{String, Any}(),
        function(entity)
            x, y = Ahorn.position(entity)

            entity.nodes = [(x + 32, y)]
        end
    )
)

const textures = Dict{String, String}(
    "Glass" => "glass",
    "Green" => "green"
)

Ahorn.editingOptions(entity::ClearPipe) = Dict{String, Any}(
    "texture" => textures
)

Ahorn.nodeLimits(entity::ClearPipe) = 1, -1

const exitTextureQuads = Dict{String, Dict{String, Tuple{Int, Int}}}(
    "up" => Dict{String, Tuple{Int, Int}}(
        "left" => (0, 3),
        "middle" => (1, 3),
        "right" => (2, 3)
    ),
    "down" => Dict{String, Tuple{Int, Int}}(
        "left" => (0, 5),
        "middle" => (1, 5),
        "right" => (2, 5)
    ),
    "left" => Dict{String, Tuple{Int, Int}}(
        "top" => (0, 0),
        "middle" => (0, 1),
        "bottom" => (0, 2)
    ),
    "right" => Dict{String, Tuple{Int, Int}}(
        "top" => (2, 0),
        "middle" => (2, 1),
        "bottom" => (2, 2)
    )
)

const straightTextureQuads = Dict{String, Dict{String, Tuple{Int, Int}}}(
    "vertical" => Dict{String, Tuple{Int, Int}}(
        "left" => (0, 4),
        "middle" => (1, 4),
        "right" => (2, 4)
    ),
    "horizontal" => Dict{String, Tuple{Int, Int}}(
        "top" => (1, 0),
        "middle" => (1, 1),
        "bottom" => (1, 2)
    )
)

const cornerTextureQuads = Dict{String, Dict{String, Tuple{Int, Int}}}(
    "upLeft" => Dict{String, Tuple{Int, Int}}(
        "inner" => (3, 0),
        "outer" => (5, 2),
        "horizontal_middle" => (3, 1),
        "horizontal_wall" => (3, 2),
        "vertical_middle" => (4, 0),
        "vertical_wall" => (5, 0)
    ),
    "upRight" => Dict{String, Tuple{Int, Int}}(
        "inner" => (8, 0),
        "outer" => (6, 2),
        "horizontal_middle" => (8, 1),
        "horizontal_wall" => (8, 2),
        "vertical_middle" => (7, 0),
        "vertical_wall" => (6, 0)
    ),
    "downRight" => Dict{String, Tuple{Int, Int}}(
        "inner" => (8, 5),
        "outer" => (6, 3),
        "horizontal_middle" => (8, 4),
        "horizontal_wall" => (8, 3),
        "vertical_middle" => (7, 5),
        "vertical_wall" => (6, 5)
    ),
    "downLeft" => Dict{String, Tuple{Int, Int}}(
        "inner" => (3, 5),
        "outer" => (5, 3),
        "horizontal_middle" => (3, 4),
        "horizontal_wall" => (3, 3),
        "vertical_middle" => (4, 5),
        "vertical_wall" => (5, 5)
    ),
)

function getDirection(x1, y1, x2, y2)
    if x2 < x1 && y1 == y2
        return "left"

    elseif x2 > x1 && y1 == y2
        return "right"

    elseif y2 < y1 && x1 == x2
        return "up"

    elseif y2 > y1 && x1 == x2
        return "down"
    end

    return "none"
end

function getCornerType(direction, nextDirection)
    if direction == "right" && nextDirection == "up" || direction == "down" && nextDirection == "left"
        return "upLeft"

    elseif direction == "down" && nextDirection == "right" || direction == "left" && nextDirection == "up"
        return "upRight"

    elseif direction == "left" && nextDirection == "down" || direction == "up" && nextDirection == "right"
        return "downRight"

    elseif direction == "up" && nextDirection == "left" || direction == "right" && nextDirection == "down"
        return "downLeft"
    end

    return "unknown"
end

function getLength(x1, y1, x2, y2, width, startNode, endNode)
    length = sqrt((x1 - x2)^2 + (y1 - y2)^2)

    if startNode
        length += width / 2
    end

    if endNode
        length -= width / 2
    end

    return floor(Int, length)
end

function getStraightDrawingInfo(x1, y1, x2, y2, width, length, direction, startNode, endNode)
    if direction == "up"
        return x2 - width / 2, y2 - (endNode ? 0 : width / 2), width, length

    elseif direction == "right"
        return x1 + (startNode ? 0 : width / 2), y1 - width / 2, width, length

    elseif direction == "down"
        return x1 - width / 2, y1 + (startNode ? 0 : width / 2), width, length

    elseif direction == "left"
        return x2 - (endNode ? 0 : width / 2), y2 - width / 2, width, length
    end

    return x1, y1, width, length
end

function getCornerDrawingInfo(cornerType, direction, width, length)
    verticalWallX, horizontalWallY = -1, -1
    offsetX, offsetY = 0, 0

    if cornerType == "upLeft"
        verticalWallX = width - 8
        horizontalWallY = width - 8

    elseif cornerType == "upRight"
        verticalWallX = 0
        horizontalWallY = width - 8

    elseif cornerType == "downRight"
        verticalWallX = 0
        horizontalWallY = 0

    elseif cornerType == "downLeft"
        verticalWallX = width - 8
        horizontalWallY = 0
    end

    if direction == "right"
        offsetX = length - width

    elseif direction == "down"
        offsetY = length - width
    end

    return verticalWallX, horizontalWallY, offsetX, offsetY
end

# -1 and -9 in loops used to get a inclusive-exclusive range
function renderStraightHorizontalSection(ctx::Ahorn.Cairo.CairoContext, texture, x, y, width, length, direction, startNode, endNode)
    for ox in 0:8:length - 1
        top = straightTextureQuads["horizontal"]["top"]
        middle = straightTextureQuads["horizontal"]["middle"]
        bottom = straightTextureQuads["horizontal"]["bottom"]

        # Right exit
        if direction == "left" && ox == length - 8 && startNode || direction == "right" && ox == length - 8 && endNode
            top = exitTextureQuads["right"]["top"]
            middle = exitTextureQuads["right"]["middle"]
            bottom = exitTextureQuads["right"]["bottom"]

        # Left exit
        elseif direction == "left" && ox == 0 && endNode || direction == "right" && ox == 0 && startNode
            top = exitTextureQuads["left"]["top"]
            middle = exitTextureQuads["left"]["middle"]
            bottom = exitTextureQuads["left"]["bottom"]
        end

        Ahorn.drawImage(ctx, texture, x + ox, y, top[1] * 8, top[2] * 8, 8, 8)
        Ahorn.drawImage(ctx, texture, x + ox, y + width - 8, bottom[1] * 8, bottom[2] * 8, 8, 8)

        for oy in 8:8:width - 9
            Ahorn.drawImage(ctx, texture, x + ox, y + oy, middle[1] * 8, middle[2] * 8, 8, 8)
        end
    end
end

# -1 and -9 in loops used to get a inclusive-exclusive range
function renderStraightVerticalSection(ctx::Ahorn.Cairo.CairoContext, texture, x, y, width, length, direction, startNode, endNode)
    for oy in 0:8:length - 1
        left = straightTextureQuads["vertical"]["left"]
        middle = straightTextureQuads["vertical"]["middle"]
        right = straightTextureQuads["vertical"]["right"]

        # Down exit
        if direction == "up" && oy == length - 8 && startNode || direction == "down" && oy == length - 8 && endNode
            left = exitTextureQuads["down"]["left"]
            middle = exitTextureQuads["down"]["middle"]
            right = exitTextureQuads["down"]["right"]

        # Up exit
        elseif direction == "up" && oy == 0 && endNode || direction == "down" && oy == 0 && startNode
            left = exitTextureQuads["up"]["left"]
            middle = exitTextureQuads["up"]["middle"]
            right = exitTextureQuads["up"]["right"]
        end

        Ahorn.drawImage(ctx, texture, x, y + oy, left[1] * 8, left[2] * 8, 8, 8)
        Ahorn.drawImage(ctx, texture, x + width - 8, y + oy, right[1] * 8, right[2] * 8, 8, 8)

        for ox in 8:8:width - 9
            Ahorn.drawImage(ctx, texture, x + ox, y + oy, middle[1] * 8, middle[2] * 8, 8, 8)
        end
    end
end

# -1 in loops used to get a inclusive-exclusive range
function renderCorner(ctx, texture, x, y, width, pipeWidth, length, direction, cornerType, startNode, endNode)
    if endNode || width < pipeWidth || cornerType == "unknown"
        return
    end

    nonWallQuad = straightTextureQuads["horizontal"]["middle"]
    innerCornerQuad = cornerTextureQuads[cornerType]["inner"]
    outerCornerQuad = cornerTextureQuads[cornerType]["outer"]
    verticalWallQuad = cornerTextureQuads[cornerType]["vertical_wall"]
    horizontalWallQuad = cornerTextureQuads[cornerType]["horizontal_wall"]

    verticalWallX, horizontalWallY, offsetX, offsetY = getCornerDrawingInfo(cornerType, direction, pipeWidth, length)

    for ox in 0:8:pipeWidth - 1
        for oy in 0:8:pipeWidth - 1
            quad = nonWallQuad

            onVertical = ox == verticalWallX
            onHorizontal = oy == horizontalWallY
            innerCorner = ox == abs(verticalWallX - pipeWidth + 8) && oy == abs(horizontalWallY - pipeWidth + 8)

            if innerCorner
                quad = innerCornerQuad

            elseif onVertical && onHorizontal
                quad = outerCornerQuad

            elseif onVertical
                quad = verticalWallQuad

            elseif onHorizontal
                quad = horizontalWallQuad
            end

            Ahorn.drawImage(ctx, texture, x + ox + offsetX, y + oy + offsetY, quad[1] * 8, quad[2] * 8, 8, 8)
        end
    end
end

function renderStraightSection(ctx::Ahorn.Cairo.CairoContext, entity::Maple.Entity, texture::String, px::Int, py::Int, nx::Int, ny::Int, nnx::Int, nny::Int, width::Int, startNode::Bool, endNode::Bool)
    direction = getDirection(px, py, nx, ny)
    nextDirection = getDirection(nx, ny, nnx, nny)
    cornerType = getCornerType(direction, nextDirection)
    pipeLength = getLength(px, py, nx, ny, width, startNode, endNode)

    vertical = direction == "up" || direction == "down"
    horizontal = direction == "left" || direction == "right"

    drawX, drawY, drawWidth, drawLength = getStraightDrawingInfo(px, py, nx, ny, width, pipeLength, direction, startNode, endNode)
    straightLength = !endNode && cornerType != "unknown" ? drawLength - drawWidth : drawLength
    drawingOffset = !endNode && (direction == "up" || direction == "left") ? drawWidth : 0

    if horizontal
        renderStraightHorizontalSection(ctx, texture, drawX + drawingOffset, drawY, drawWidth, straightLength, direction, startNode, endNode)
        renderCorner(ctx, texture, drawX, drawY, drawWidth, width, pipeLength, direction, cornerType, startNode, endNode)

    elseif vertical
        renderStraightVerticalSection(ctx, texture, drawX, drawY + drawingOffset, drawWidth, straightLength, direction, startNode, endNode)
        renderCorner(ctx, texture, drawX, drawY, drawWidth, width, pipeLength, direction, cornerType, startNode, endNode)
    end
end

function renderPipe(ctx::Ahorn.Cairo.CairoContext, entity::Maple.Entity)
    type = get(entity, "texture", "glass")
    texture = "objects/pandorasBox/clearPipe/$type/pipe"

    hasSolids =  get(entity, "hasPipeSolids", true)
    width = Int(get(entity, "pipeWidth", 32))

    px, py = Ahorn.position(entity)
    nodes = get(entity, "nodes", [])
    nodesWithPosition = vcat([(px, py)], nodes)

    if hasSolids
        for (i, node) in enumerate(nodes)
            startNode, endNode = i == 1, i == length(nodes)
            nx, ny = node
            nnx, nny = endNode ? (-1, -1) : Int.(nodes[i + 1])

            renderStraightSection(ctx, entity, texture, px, py, nx, ny, nnx, nny, width, startNode, endNode)

            px, py = nx, ny
        end
    end

    Ahorn.drawLines(ctx, nodesWithPosition, Ahorn.colors.selection_selected_fc, thickness=2)
end

function Ahorn.selection(entity::ClearPipe)
    nodes = get(entity.data, "nodes", ())
    x, y = Ahorn.position(entity)

    res = Ahorn.Rectangle[Ahorn.Rectangle(x - 6, y - 6, 12, 12)]

    for node in nodes
        nx, ny = Int.(node)

        push!(res, Ahorn.Rectangle(nx - 6, ny - 6, 12, 12))
    end

    return res
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ClearPipe, room::Maple.Room)
    renderPipe(ctx, entity)
end

end