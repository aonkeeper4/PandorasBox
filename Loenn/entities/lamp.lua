local drawableSprite = require("structs.drawable_sprite")

local lamp = {}

lamp.name = "pandorasBox/lamp"
lamp.depth = 5
lamp.fieldInformation = {
    baseColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    lightColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    lightStartRadius = {
        fieldType = "integer",
    },
    lightEndRadius = {
        fieldType = "integer",
    },
}
lamp.placements = {
    name = "lamp",
    data = {
        inverted = false,
        flag = "",
        baseColor = "White",
        lightColor = "White",
        lightMode = "Smooth",
        lightStartRadius = 48,
        lightEndRadius = 64
    }
}

local gemTexture = "objects/pandorasBox/lamp/idle0"
local baseTexture = "objects/pandorasBox/lamp/base"

function lamp.sprite(room, entity)
    local colorRaw = entity.baseColor or "White"

    local baseSprite = drawableSprite.fromTexture(baseTexture, entity)
    local gemSprite = drawableSprite.fromTexture(gemTexture, entity)

    baseSprite:setColor(colorRaw)

    return {
        baseSprite,
        gemSprite
    }
end

return lamp