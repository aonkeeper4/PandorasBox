-- TODO - Lines for render?

local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")

local clearPipe = {}

clearPipe.name = "pandorasBox/clearPipe"
clearPipe.nodeLimits = {1, -1}
clearPipe.minimumSize = {16, 16}
clearPipe.nodeVisibility = "never"
clearPipe.nodeLineRenderType = "line"
clearPipe.depth = -11000
clearPipe.fieldInformation = {
    surfaceSound = {
        fieldType = "integer",
    },
    pipeWidth = {
        fieldType = "integer",
    },
    texture = {
        options = {
            Glass = "glass",
            Green = "green"
        }
    }
}


clearPipe.placements = {
    name = "glass",
    data = {
        texture = "glass",
        surfaceSound = -1,
        transportSpeed = 175.0,
        pipeWidth = 32,
        useLegacyMovement = false,
    }
}

local exitTextureQuads = {
    up = {
        left = {0, 3},
        middle = {1, 3},
        right = {2, 3}
    },
    down = {
        left = {0, 5},
        middle = {1, 5},
        right = {2, 5}
    },
    left = {
        top = {0, 0},
        middle = {0, 1},
        bottom = {0, 2}
    },
    right = {
        top = {2, 0},
        middle = {2, 1},
        bottom = {2, 2}
    }
}

local straightTextureQuads = {
    vertical = {
        left = {0, 4},
        middle = {1, 4},
        right = {2, 4}
    },
    horizontal = {
        top = {1, 0},
        middle = {1, 1},
        bottom = {1, 2}
    }
}

local cornerTextureQuads = {
    upLeft = {
        inner = {3, 0},
        outer = {5, 2},
        horizontal_middle = {3, 1},
        horizontal_wall = {3, 2},
        vertical_middle = {4, 0},
        vertical_wall = {5, 0}
    },
    upRight = {
        inner = {8, 0},
        outer = {6, 2},
        horizontal_middle = {8, 1},
        horizontal_wall = {8, 2},
        vertical_middle = {7, 0},
        vertical_wall = {6, 0}
    },
    downRight = {
        inner = {8, 5},
        outer = {6, 3},
        horizontal_middle = {8, 4},
        horizontal_wall = {8, 3},
        vertical_middle = {7, 5},
        vertical_wall = {6, 5}
    },
    downLeft = {
        inner = {3, 5},
        outer = {5, 3},
        horizontal_middle = {3, 4},
        horizontal_wall = {3, 3},
        vertical_middle = {4, 5},
        vertical_wall = {5, 5}
    },
}

local function getDirection(x1, y1, x2, y2)
    if x2 < x1 and y1 == y2 then
        return "left"

    elseif x2 > x1 and y1 == y2 then
        return "right"

    elseif y2 < y1 and x1 == x2 then
        return "up"

    elseif y2 > y1 and x1 == x2 then
        return "down"
    end

    return "none"
end

local function getCornerType(direction, nextDirection)
    if direction == "right" and nextDirection == "up" or direction == "down" and nextDirection == "left" then
        return "upLeft"

    elseif direction == "down" and nextDirection == "right" or direction == "left" and nextDirection == "up" then
        return "upRight"

    elseif direction == "left" and nextDirection == "down" or direction == "up" and nextDirection == "right" then
        return "downRight"

    elseif direction == "up" and nextDirection == "left" or direction == "right" and nextDirection == "down" then
        return "downLeft"
    end

    return "unknown"
end

local function getLength(x1, y1, x2, y2, width, startNode, endNode)
    local length = math.sqrt((x1 - x2)^2 + (y1 - y2)^2)

    if startNode then
        length += width / 2
    end

    if endNode then
        length -= width / 2
    end

    return math.floor(length)
end

local function getStraightDrawingInfo(x1, y1, x2, y2, width, length, direction, startNode, endNode)
    if direction == "up" then
        return x2 - width / 2, y2 - (endNode and 0 or width / 2), width, length

    elseif direction == "right" then
        return x1 + (startNode and 0 or width / 2), y1 - width / 2, width, length

    elseif direction == "down" then
        return x1 - width / 2, y1 + (startNode and 0 or width / 2), width, length

    elseif direction == "left" then
        return x2 - (endNode and 0 or width / 2), y2 - width / 2, width, length
    end

    return x1, y1, width, length
end

local function getCornerDrawingInfo(cornerType, direction, width, length)
    local verticalWallX, horizontalWallY = -1, -1
    local offsetX, offsetY = 0, 0

    if cornerType == "upLeft" then
        verticalWallX = width - 8
        horizontalWallY = width - 8

    elseif cornerType == "upRight" then
        verticalWallX = 0
        horizontalWallY = width - 8

    elseif cornerType == "downRight" then
        verticalWallX = 0
        horizontalWallY = 0

    elseif cornerType == "downLeft" then
        verticalWallX = width - 8
        horizontalWallY = 0
    end

    if direction == "right" then
        offsetX = length - width

    elseif direction == "down" then
        offsetY = length - width
    end

    return verticalWallX, horizontalWallY, offsetX, offsetY
end

local function addSprite(sprites, texture, x, y, quadX, quadY)
    local sprite = drawableSprite.fromTexture(texture, {x = x, y = y})

    sprite:useRelativeQuad(quadX * 8, quadY * 8, 8, 8)
    table.insert(sprites, sprite)
end

local function addStraightHorizontalSprites(sprites, texture, x, y, width, length, direction, startNode, endNode)
    for ox = 0, length - 1, 8 do
        local top = straightTextureQuads["horizontal"]["top"]
        local middle = straightTextureQuads["horizontal"]["middle"]
        local bottom = straightTextureQuads["horizontal"]["bottom"]

        -- Right exit
        if direction == "left" and ox == length - 8 and startNode or direction == "right" and ox == length - 8 and endNode then
            top = exitTextureQuads["right"]["top"]
            middle = exitTextureQuads["right"]["middle"]
            bottom = exitTextureQuads["right"]["bottom"]

        -- Left exit
        elseif direction == "left" and ox == 0 and endNode or direction == "right" and ox == 0 and startNode then
            top = exitTextureQuads["left"]["top"]
            middle = exitTextureQuads["left"]["middle"]
            bottom = exitTextureQuads["left"]["bottom"]
        end

        addSprite(sprites, texture, x + ox, y, top[1], top[2])
        addSprite(sprites, texture, x + ox, y + width - 8, bottom[1], bottom[2])

        for oy = 8, width - 9, 8 do
            addSprite(sprites, texture, x + ox, y + oy, middle[1], middle[2])
        end
    end
end

local function addStraightVerticalSprites(sprites, texture, x, y, width, length, direction, startNode, endNode)
    for oy = 0, length - 1, 8 do
        local left = straightTextureQuads["vertical"]["left"]
        local middle = straightTextureQuads["vertical"]["middle"]
        local right = straightTextureQuads["vertical"]["right"]

        -- Down exit
        if direction == "up" and oy == length - 8 and startNode or direction == "down" and oy == length - 8 and endNode then
            left = exitTextureQuads["down"]["left"]
            middle = exitTextureQuads["down"]["middle"]
            right = exitTextureQuads["down"]["right"]

        -- Up exit
        elseif direction == "up" and oy == 0 and endNode or direction == "down" and oy == 0 and startNode then
            left = exitTextureQuads["up"]["left"]
            middle = exitTextureQuads["up"]["middle"]
            right = exitTextureQuads["up"]["right"]
        end

        addSprite(sprites, texture, x, y + oy, left[1], left[2])
        addSprite(sprites, texture, x + width - 8, y + oy, right[1], right[2])

        for ox = 8, width - 9, 8 do
            addSprite(sprites, texture, x + ox, y + oy, middle[1], middle[2])
        end
    end
end

local function addCornerSprites(sprites, texture, x, y, width, pipeWidth, length, direction, cornerType, startNode, endNode)
    if endNode or width < pipeWidth or cornerType == "unknown" then
        return
    end

    local nonWallQuad = straightTextureQuads["horizontal"]["middle"]
    local innerCornerQuad = cornerTextureQuads[cornerType]["inner"]
    local outerCornerQuad = cornerTextureQuads[cornerType]["outer"]
    local verticalWallQuad = cornerTextureQuads[cornerType]["vertical_wall"]
    local horizontalWallQuad = cornerTextureQuads[cornerType]["horizontal_wall"]

    local verticalWallX, horizontalWallY, offsetX, offsetY = getCornerDrawingInfo(cornerType, direction, pipeWidth, length)

    for ox = 0, pipeWidth - 1, 8 do
        for oy = 0, pipeWidth - 1, 8 do
            local quad = nonWallQuad

            local onVertical = ox == verticalWallX
            local onHorizontal = oy == horizontalWallY
            local innerCorner = ox == math.abs(verticalWallX - pipeWidth + 8) and oy == math.abs(horizontalWallY - pipeWidth + 8)

            if innerCorner then
                quad = innerCornerQuad

            elseif onVertical and onHorizontal then
                quad = outerCornerQuad

            elseif onVertical then
                quad = verticalWallQuad

            elseif onHorizontal then
                quad = horizontalWallQuad
            end

            addSprite(sprites, texture, x + ox + offsetX, y + oy + offsetY, quad[1], quad[2])
        end
    end
end

local function addPipeSectionSprites(sprites, entity, texture, px, py, nx, ny, nnx, nny, width, startNode, endNode)
    local direction = getDirection(px, py, nx, ny)
    local nextDirection = getDirection(nx, ny, nnx, nny)
    local cornerType = getCornerType(direction, nextDirection)
    local pipeLength = getLength(px, py, nx, ny, width, startNode, endNode)

    local vertical = direction == "up" or direction == "down"
    local horizontal = direction == "left" or direction == "right"

    local drawX, drawY, drawWidth, drawLength = getStraightDrawingInfo(px, py, nx, ny, width, pipeLength, direction, startNode, endNode)
    local straightLength = not endNode and cornerType ~= "unknown" and drawLength - drawWidth or drawLength
    local drawingOffset = not endNode and (direction == "up" or direction == "left") and drawWidth or 0

    if horizontal then
        addStraightHorizontalSprites(sprites, texture, drawX + drawingOffset, drawY, drawWidth, straightLength, direction, startNode, endNode)
        addCornerSprites(sprites, texture, drawX, drawY, drawWidth, width, pipeLength, direction, cornerType, startNode, endNode)

    elseif vertical then
        addStraightVerticalSprites(sprites, texture, drawX, drawY + drawingOffset, drawWidth, straightLength, direction, startNode, endNode)
        addCornerSprites(sprites, texture, drawX, drawY, drawWidth, width, pipeLength, direction, cornerType, startNode, endNode)
    end
end

local function addPipeSprites(sprites, entity)
    local variant = entity.texture or "glass"
    local texture = string.format("objects/pandorasBox/clearPipe/%s/pipe", variant)

    local hasSolids = entity.hasPipeSolids or entity.hasPipeSolids == nil
    local width = entity.pipeWidth or 32

    local px, py = entity.x, entity.y
    local nodes = entity.nodes or {}

    if hasSolids then
        for i, node in ipairs(nodes) do
            local startNode, endNode = i == 1, i == #nodes
            local nx, ny = node.x, node.y
            local nnx = endNode and -1 or nodes[i + 1].x
            local nny = endNode and -1 or nodes[i + 1].y

            addPipeSectionSprites(sprites, entity, texture, px, py, nx, ny, nnx, nny, width, startNode, endNode)

            px, py = nx, ny
        end
    end

    return sprites
end

function clearPipe.sprite(room, entity)
    return addPipeSprites({}, entity)
end

function clearPipe.selection(room, entity)
    local nodes = entity.nodes or {}
    local nodeRectangles = {}
    local mainRectangle = utils.rectangle(entity.x - 2, entity.y - 2, 5, 5)

    for i, node in ipairs(nodes) do
        nodeRectangles[i] = utils.rectangle(node.x - 2, node.y - 2, 5, 5)
    end

    return mainRectangle, nodeRectangles
end

return clearPipe