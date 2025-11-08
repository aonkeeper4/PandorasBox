local drawableSprite = require("structs.drawable_sprite")

local shell = {}

local defaultTextures = {
    Koopa = "koopa",
    Beetle = "beetle",
    Spiny = "spiny",
    ["Bowser Jr."] = "bowserjr",
}

shell.name = "pandorasBox/shell"
shell.depth = 0
shell.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
    },
    direction = {
        fieldType = "integer"
    },
    texture = {
        options = defaultTextures
    }
}
shell.placements = {
    name = "shell",
    data = {
        texture = "koopa",
        lights = false,
        color = "Green",
        colorSpeed = 0.8,
        dangerous = false,
        grabbable = true,
        direction = 0
    }
}

local shellBase = "objects/pandorasBox/shells/%s/shell_idle00"
local decoShellBase = "objects/pandorasBox/shells/%s/deco_idle00"

local function getShellTextures(entity)
    local texture = entity.texture or "koopa"

    return string.format(shellBase, texture), string.format(decoShellBase, texture)
end

function shell.sprite(room, entity)
    local colorRaw = entity.color or "Green"
    local shellTexture, decoTexture = getShellTextures(entity)

    -- Don't want color to carry over
    local position = {
        x = entity.x,
        y = entity.y
    }

    local shellSprite = drawableSprite.fromTexture(shellTexture, position)
    local decoSprite = drawableSprite.fromTexture(decoTexture, position)

    shellSprite:setColor(colorRaw)

    return {
        shellSprite,
        decoSprite
    }
end

return shell