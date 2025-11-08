local drawableSprite = require("structs.drawable_sprite")

local lightSensor = {}

local modes = {
    "Continuous",
    "HitOnce"
}

lightSensor.name = "pandorasBox/laserSensor"
lightSensor.depth = 200
lightSensor.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
    }
}
lightSensor.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
    },
    mode = {
        options = modes,
        editable = false
    }
}
lightSensor.placements = {
    name = "sensor",
    data = {
        flag = "",
        color = "White",
        blockLight = true,
        mode = "Continuous"
    }
}

local orbTexture = "objects/pandorasBox/laser/sensor/orb"
local metalRingTexture = "objects/pandorasBox/laser/sensor/metal_ring"
local lightRingTexture = "objects/pandorasBox/laser/sensor/light_ring"


function lightSensor.sprite(room, entity)
    local colorRaw = entity.color or "White"
    -- Don't want color to carry over
    local position = {
        x = entity.x,
        y = entity.y
    }

    local metalRingSprite = drawableSprite.fromTexture(metalRingTexture, position)
    local lightRingSprite = drawableSprite.fromTexture(lightRingTexture, position)
    local orbSprite = drawableSprite.fromTexture(orbTexture, position)

    lightRingSprite:setColor(colorRaw)

    return {
        metalRingSprite,
        lightRingSprite,
        orbSprite
    }
end

return lightSensor